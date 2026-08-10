using Illumin360.Professionals.Application.Abstractions;
using Illumin360.Resume;
using Illumin360.SharedKernel;
using Illumin360.Storage;

namespace Illumin360.Professionals.Application.Professionals;

/// <summary>Skills detected in the current professional's uploaded CV.</summary>
/// <param name="Skills">Detected skill names.</param>
public sealed record CvSkillsDto(IReadOnlyList<string> Skills);

/// <summary>Parses the current ("me") professional's stored CV and detects skills.</summary>
public sealed record ParseCvSkillsQuery : IQuery<CvSkillsDto>;

/// <summary>Handles <see cref="ParseCvSkillsQuery"/> by extracting text from the stored CV and detecting skills.</summary>
/// <param name="repository">The professional repository.</param>
/// <param name="storage">Object storage.</param>
public sealed class ParseCvSkillsQueryHandler(IProfessionalRepository repository, IObjectStorage storage)
    : IQueryHandler<ParseCvSkillsQuery, CvSkillsDto>
{
    private readonly IProfessionalRepository _repository = repository;
    private readonly IObjectStorage _storage = storage;

    /// <inheritdoc />
    public async Task<Result<CvSkillsDto>> HandleAsync(ParseCvSkillsQuery query, CancellationToken cancellationToken)
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

        await using var content = download.Content;
        var text = ResumeTextExtractor.Extract(content, me.CvContentType ?? download.ContentType);
        return new CvSkillsDto(SkillExtractor.Detect(text));
    }
}
