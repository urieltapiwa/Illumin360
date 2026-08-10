using Illumin360.Resume;
using Illumin360.SharedKernel;
using Illumin360.Storage;
using Illumin360.Students.Application.Abstractions;
using Illumin360.Students.Domain;

namespace Illumin360.Students.Application.Students;

/// <summary>The outcome of parsing a CV and applying detected skills to the student profile.</summary>
/// <param name="Detected">All skills detected in the CV.</param>
/// <param name="Added">The subset newly added to the profile (not already present).</param>
public sealed record AppliedSkillsDto(IReadOnlyList<string> Detected, IReadOnlyList<string> Added);

/// <summary>Parses the current ("me") student's CV and adds any newly detected skills to the profile.</summary>
public sealed record ApplyCvSkillsCommand : ICommand<AppliedSkillsDto>;

/// <summary>Handles <see cref="ApplyCvSkillsCommand"/>.</summary>
/// <param name="repository">The student repository.</param>
/// <param name="storage">Object storage.</param>
public sealed class ApplyCvSkillsCommandHandler(IStudentRepository repository, IObjectStorage storage)
    : ICommandHandler<ApplyCvSkillsCommand, AppliedSkillsDto>
{
    // Newly-detected skills land at a neutral, self-assessed starting level until edited.
    private const int DefaultLevel = 60;

    private readonly IStudentRepository _repository = repository;
    private readonly IObjectStorage _storage = storage;

    /// <inheritdoc />
    public async Task<Result<AppliedSkillsDto>> HandleAsync(ApplyCvSkillsCommand command, CancellationToken cancellationToken)
    {
        var me = await GetCvMetadataQueryHandler.ResolveMeAsync(_repository, cancellationToken).ConfigureAwait(false);
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
            await _repository.GetSkillNamesAsync(me.Id, cancellationToken).ConfigureAwait(false),
            StringComparer.OrdinalIgnoreCase);

        var added = detected.Where(s => !existing.Contains(s)).ToList();
        var sort = existing.Count;
        foreach (var name in added)
        {
            _repository.AddSkill(new StudentSkill(Guid.NewGuid(), me.Id, name, DefaultLevel, sort++));
        }

        if (added.Count > 0)
        {
            await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new AppliedSkillsDto(detected, added);
    }
}
