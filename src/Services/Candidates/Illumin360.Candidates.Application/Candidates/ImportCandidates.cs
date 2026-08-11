using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;
using IntegrationEvents = Illumin360.Candidates.IntegrationEvents;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>The outcome of a bulk candidate import.</summary>
/// <param name="Created">How many candidates were registered.</param>
/// <param name="Skipped">How many rows were skipped as duplicates.</param>
/// <param name="Errors">Header/row problems (parse + validation), human-readable.</param>
public sealed record ImportResultDto(int Created, int Skipped, IReadOnlyList<string> Errors);

/// <summary>Command: bulk-import candidates from CSV text.</summary>
/// <param name="Csv">The raw CSV (header + rows).</param>
public sealed record ImportCandidatesCommand(string? Csv) : ICommand<ImportResultDto>;

/// <summary>Handles <see cref="ImportCandidatesCommand"/> — parses CSV and registers each new candidate.</summary>
public sealed class ImportCandidatesCommandHandler(
    ICandidateRepository repository,
    IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<ImportCandidatesCommand, ImportResultDto>
{
    private readonly ICandidateRepository _repository = repository;
    private readonly IIntegrationEventPublisher _eventPublisher = eventPublisher;

    private static string Key(string first, string last, string city)
        => $"{first.Trim()} {last.Trim()}|{city.Trim()}".ToLowerInvariant();

    /// <inheritdoc />
    public async Task<Result<ImportResultDto>> HandleAsync(ImportCandidatesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var parsed = CandidateCsv.Parse(command.Csv);
        var errors = new List<string>(parsed.Errors);

        if (parsed.Rows.Count == 0)
        {
            return new ImportResultDto(0, 0, errors);
        }

        // Dedupe against existing candidates and within the batch, by normalised name + city.
        var existing = await _repository.ListAllAsync(cancellationToken).ConfigureAwait(false);
        var seen = existing.Select(c => Key(c.FirstName, c.LastName, c.City)).ToHashSet();

        var created = 0;
        var skipped = 0;
        var registered = new List<Candidate>();

        foreach (var row in parsed.Rows)
        {
            var key = Key(row.FirstName, row.LastName, row.City);
            if (!seen.Add(key))
            {
                skipped++;
                continue;
            }

            var availability = AvailabilityStatus.ActivelyLooking;
            if (!string.IsNullOrWhiteSpace(row.Availability)
                && !Enum.TryParse(row.Availability, ignoreCase: true, out availability))
            {
                errors.Add($"Line {row.Line}: '{row.Availability}' is not a valid availability status.");
                continue;
            }

            var registration = Candidate.Register(row.FirstName, row.LastName, row.City, row.Nationality, availability, row.Headline);
            if (registration.IsFailure)
            {
                errors.Add($"Line {row.Line}: {registration.Error!.Message}");
                continue;
            }

            _repository.Add(registration.Value!);
            registered.Add(registration.Value!);
            created++;
        }

        if (created > 0)
        {
            foreach (var candidate in registered)
            {
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
            }

            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new ImportResultDto(created, skipped, errors);
    }
}
