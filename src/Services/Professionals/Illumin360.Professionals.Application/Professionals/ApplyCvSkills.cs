using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Professionals.Domain;
using Illumin360.Resume;
using Illumin360.SharedKernel;
using Illumin360.Storage;

namespace Illumin360.Professionals.Application.Professionals;

/// <summary>The outcome of parsing a CV and applying detected skills to the profile.</summary>
/// <param name="Detected">All skills detected in the CV.</param>
/// <param name="Added">The subset newly added to the profile (not already present).</param>
public sealed record AppliedSkillsDto(IReadOnlyList<string> Detected, IReadOnlyList<string> Added);

/// <summary>Parses the current ("me") professional's CV and adds any newly detected skills to the profile.</summary>
public sealed record ApplyCvSkillsCommand : ICommand<AppliedSkillsDto>;

/// <summary>Handles <see cref="ApplyCvSkillsCommand"/>.</summary>
/// <param name="repository">The professional repository.</param>
/// <param name="storage">Object storage.</param>
public sealed class ApplyCvSkillsCommandHandler(IProfessionalRepository repository, IObjectStorage storage)
    : ICommandHandler<ApplyCvSkillsCommand, AppliedSkillsDto>
{
    // Newly-detected skills land at a neutral, self-assessed starting level until edited.
    private const int DefaultLevel = 60;
    private const string DefaultTrend = "steady";

    private readonly IProfessionalRepository _repository = repository;
    private readonly IObjectStorage _storage = storage;

    /// <inheritdoc />
    public async Task<Result<AppliedSkillsDto>> HandleAsync(ApplyCvSkillsCommand command, CancellationToken cancellationToken)
    {
        var meId = await _repository.GetDefaultProfessionalIdAsync(cancellationToken).ConfigureAwait(false);
        if (meId is not { } id)
        {
            return Error.NotFound("professional.not_found", "No professional profile found.");
        }

        var me = await _repository.GetTrackedAsync(id, cancellationToken).ConfigureAwait(false);
        if (me is null || !me.HasCv)
        {
            return Error.NotFound("cv.not_found", "No CV has been uploaded.");
        }

        var download = await _storage.GetAsync(CvStorage.Bucket, me.CvObjectKey!, cancellationToken).ConfigureAwait(false);
        if (download is null)
        {
            return Error.NotFound("cv.not_found", "The stored CV could not be found.");
        }

        List<string> detected;
        await using (var content = download.Content)
        {
            var text = ResumeTextExtractor.Extract(content, me.CvContentType ?? download.ContentType);
            detected = [.. SkillExtractor.Detect(text)];
        }

        var existing = new HashSet<string>(
            await _repository.GetSkillNamesAsync(id, cancellationToken).ConfigureAwait(false),
            StringComparer.OrdinalIgnoreCase);

        var added = detected.Where(s => !existing.Contains(s)).ToList();
        var sort = existing.Count;
        foreach (var name in added)
        {
            _repository.AddSkill(new ProfessionalSkill(Guid.NewGuid(), id, name, DefaultLevel, DefaultTrend, sort++));
        }

        if (added.Count > 0)
        {
            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new AppliedSkillsDto(detected, added);
    }
}
