using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Domain;
using Illumin360.Resume;
using Illumin360.SharedKernel;
using Illumin360.Storage;
using IntegrationEvents = Illumin360.Candidates.IntegrationEvents;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>Pure helpers for deriving candidate fields from an inbound résumé email.</summary>
public static class EmailIntake
{
    private const string Unknown = "Unknown";

    /// <summary>
    /// Derives a (first, last) name from the sender's display name, falling back to the attachment file
    /// name (sans extension). Placeholders are used when a part can't be determined so the record can be
    /// created as a stub for a recruiter to complete.
    /// </summary>
    /// <param name="fromName">The email "From" display name, if any.</param>
    /// <param name="fileName">The attachment file name, if any.</param>
    /// <returns>A (first, last) pair, never empty.</returns>
    public static (string First, string Last) DeriveName(string? fromName, string? fileName)
    {
        var source = !string.IsNullOrWhiteSpace(fromName)
            ? fromName!
            : StripExtension(fileName);

        var words = (source ?? string.Empty)
            .Replace('_', ' ')
            .Replace('.', ' ')
            .Replace('-', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return words.Length switch
        {
            0 => ("(unknown)", "(candidate)"),
            1 => (words[0], "(unknown)"),
            _ => (words[0], string.Join(' ', words[1..])),
        };
    }

    /// <summary>Builds a headline from detected skills (top few), falling back to the email subject.</summary>
    /// <param name="skills">Detected skills.</param>
    /// <param name="subject">Email subject.</param>
    /// <returns>A headline (≤ 150 chars), or null if nothing usable.</returns>
    public static string? BuildHeadline(IReadOnlyList<string> skills, string? subject)
    {
        ArgumentNullException.ThrowIfNull(skills);
        var headline = skills.Count > 0
            ? string.Join(", ", skills.Take(6))
            : subject?.Trim();

        if (string.IsNullOrWhiteSpace(headline))
        {
            return null;
        }

        return headline.Length > 150 ? headline[..150] : headline;
    }

    /// <summary>The placeholder used for unknown city/nationality on an email-intake stub.</summary>
    public static string UnknownField => Unknown;

    private static string StripExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        var dot = fileName.LastIndexOf('.');
        return dot > 0 ? fileName[..dot] : fileName;
    }
}

/// <summary>The outcome of an email résumé intake.</summary>
/// <param name="Created">Whether a new candidate was created.</param>
/// <param name="CandidateId">The candidate id (new or existing duplicate).</param>
/// <param name="CandidateName">The derived candidate name.</param>
/// <param name="Skipped">Set when skipped as a duplicate.</param>
public sealed record EmailIntakeResultDto(bool Created, Guid? CandidateId, string CandidateName, string? Skipped);

/// <summary>Command: ingest a résumé emailed to the company inbox into a candidate record.</summary>
/// <param name="FromName">Sender display name.</param>
/// <param name="FromEmail">Sender email.</param>
/// <param name="Subject">Email subject.</param>
/// <param name="FileName">Attachment file name.</param>
/// <param name="ContentBase64">Base64-encoded attachment bytes.</param>
/// <param name="ContentType">Attachment MIME type.</param>
public sealed record IngestEmailResumeCommand(string? FromName, string? FromEmail, string? Subject, string? FileName, string? ContentBase64, string? ContentType) : ICommand<EmailIntakeResultDto>;

/// <summary>Handles <see cref="IngestEmailResumeCommand"/> — parses the attachment and creates a candidate stub.</summary>
public sealed class IngestEmailResumeCommandHandler(
    ICandidateRepository repository,
    IObjectStorage storage,
    IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<IngestEmailResumeCommand, EmailIntakeResultDto>
{
    private readonly ICandidateRepository _repository = repository;
    private readonly IObjectStorage _storage = storage;
    private readonly IIntegrationEventPublisher _eventPublisher = eventPublisher;

    private static string Key(string first, string last, string city)
        => $"{first.Trim()} {last.Trim()}|{city.Trim()}".ToLowerInvariant();

    /// <inheritdoc />
    public async Task<Result<EmailIntakeResultDto>> HandleAsync(IngestEmailResumeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        byte[]? bytes = null;
        if (!string.IsNullOrWhiteSpace(command.ContentBase64))
        {
            try
            {
                bytes = Convert.FromBase64String(command.ContentBase64);
            }
            catch (FormatException)
            {
                return Error.Validation("intake.bad_base64", "The attachment is not valid base64.");
            }
        }

        // Parse the résumé (best-effort) to detect skills for the headline.
        IReadOnlyList<string> skills = [];
        if (bytes is { Length: > 0 } && !string.IsNullOrWhiteSpace(command.ContentType))
        {
            try
            {
                using var stream = new MemoryStream(bytes);
                var text = ResumeTextExtractor.Extract(stream, command.ContentType!);
                skills = SkillExtractor.Detect(text);
            }
            catch (NotSupportedException)
            {
                // Unparseable attachment type — still create the stub from the email metadata.
            }
        }

        var (first, last) = EmailIntake.DeriveName(command.FromName, command.FileName);
        var city = EmailIntake.UnknownField;

        // Dedupe against existing candidates by name + city (a re-sent email won't create a duplicate).
        var existing = await _repository.ListAllAsync(cancellationToken).ConfigureAwait(false);
        var match = existing.FirstOrDefault(c => Key(c.FirstName, c.LastName, c.City) == Key(first, last, city));
        if (match is not null)
        {
            return new EmailIntakeResultDto(false, match.Id.Value, $"{first} {last}", "duplicate");
        }

        var registration = Candidate.Register(first, last, city, EmailIntake.UnknownField, AvailabilityStatus.ActivelyLooking, EmailIntake.BuildHeadline(skills, command.Subject));
        if (registration.IsFailure)
        {
            return registration.Error!;
        }

        var candidate = registration.Value!;
        _repository.Add(candidate);

        // Attach the résumé to the candidate's record when it's a supported CV type.
        if (bytes is { Length: > 0 }
            && !string.IsNullOrWhiteSpace(command.ContentType)
            && CvStorage.AllowedTypes.Contains(command.ContentType!)
            && bytes.Length <= CvStorage.MaxBytes)
        {
            var ext = command.ContentType switch
            {
                "application/pdf" => ".pdf",
                "application/msword" => ".doc",
                _ => ".docx",
            };
            var key = $"candidates/{candidate.Id.Value}/cv{ext}";
            using var upload = new MemoryStream(bytes);
            await _storage.PutAsync(CvStorage.Bucket, key, upload, command.ContentType!, cancellationToken).ConfigureAwait(false);
            candidate.SetCv(key, string.IsNullOrWhiteSpace(command.FileName) ? $"cv{ext}" : command.FileName!, command.ContentType!, bytes.Length, DateTimeOffset.UtcNow);
        }

        foreach (var domainEvent in candidate.DomainEvents)
        {
            if (domainEvent is CandidateRegistered ev)
            {
                await _eventPublisher.PublishAsync(
                    new IntegrationEvents.CandidateRegistered(ev.CandidateId.Value, ev.OccurredOn),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        candidate.ClearDomainEvents();
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new EmailIntakeResultDto(true, candidate.Id.Value, $"{first} {last}", null);
    }
}
