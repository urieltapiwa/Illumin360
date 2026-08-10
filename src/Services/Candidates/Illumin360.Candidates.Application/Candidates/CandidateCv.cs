using Illumin360.Candidates.Application.Abstractions;
using Illumin360.Candidates.Domain;
using Illumin360.SharedKernel;
using Illumin360.Storage;

namespace Illumin360.Candidates.Application.Candidates;

/// <summary>Metadata describing a candidate's uploaded CV.</summary>
/// <param name="FileName">Original file name.</param>
/// <param name="ContentType">MIME type.</param>
/// <param name="Size">Size in bytes.</param>
/// <param name="UploadedAt">Upload timestamp (UTC).</param>
public sealed record CvDto(string FileName, string ContentType, long Size, DateTimeOffset UploadedAt);

/// <summary>A CV's content stream and metadata, for download.</summary>
/// <param name="Content">Content stream (caller disposes).</param>
/// <param name="ContentType">MIME type.</param>
/// <param name="FileName">Original file name.</param>
public sealed record CvContent(Stream Content, string ContentType, string FileName);

/// <summary>Uploads (or replaces) a candidate's CV.</summary>
/// <param name="CandidateId">The candidate id.</param>
/// <param name="FileName">Original file name.</param>
/// <param name="ContentType">MIME type.</param>
/// <param name="Length">Size in bytes.</param>
/// <param name="Content">Content stream.</param>
public sealed record UploadCandidateCvCommand(Guid CandidateId, string FileName, string ContentType, long Length, Stream Content) : ICommand<CvDto>;

/// <summary>Reads a candidate's CV metadata.</summary>
/// <param name="CandidateId">The candidate id.</param>
public sealed record GetCandidateCvMetadataQuery(Guid CandidateId) : IQuery<CvDto>;

/// <summary>Downloads a candidate's CV content.</summary>
/// <param name="CandidateId">The candidate id.</param>
public sealed record DownloadCandidateCvQuery(Guid CandidateId) : IQuery<CvContent>;

/// <summary>Constants + validation for CV storage.</summary>
public static class CvStorage
{
    /// <summary>The bucket CVs are stored in.</summary>
    public const string Bucket = "cvs";

    /// <summary>Maximum accepted CV size (5 MB).</summary>
    public const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>Accepted CV content types (PDF, DOC, DOCX).</summary>
    public static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    };
}

/// <summary>Handles <see cref="UploadCandidateCvCommand"/>.</summary>
/// <param name="repository">The candidate repository.</param>
/// <param name="storage">Object storage.</param>
public sealed class UploadCandidateCvCommandHandler(ICandidateRepository repository, IObjectStorage storage)
    : ICommandHandler<UploadCandidateCvCommand, CvDto>
{
    private readonly ICandidateRepository _repository = repository;
    private readonly IObjectStorage _storage = storage;

    /// <inheritdoc />
    public async Task<Result<CvDto>> HandleAsync(UploadCandidateCvCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Length <= 0)
        {
            return Error.Validation("cv.empty", "The uploaded file is empty.");
        }

        if (command.Length > CvStorage.MaxBytes)
        {
            return Error.Validation("cv.too_large", "The CV must be 5 MB or smaller.");
        }

        if (!CvStorage.AllowedTypes.Contains(command.ContentType))
        {
            return Error.Validation("cv.unsupported_type", "The CV must be a PDF or Word document.");
        }

        var candidate = await _repository.GetByIdAsync(new CandidateId(command.CandidateId), cancellationToken).ConfigureAwait(false);
        if (candidate is null)
        {
            return Error.NotFound("candidate.not_found", "No matching candidate was found.");
        }

        var ext = command.ContentType switch
        {
            "application/pdf" => ".pdf",
            "application/msword" => ".doc",
            _ => ".docx",
        };
        var key = $"candidates/{command.CandidateId}/cv{ext}";

        await _storage.PutAsync(CvStorage.Bucket, key, command.Content, command.ContentType, cancellationToken).ConfigureAwait(false);

        var uploadedAt = DateTimeOffset.UtcNow;
        candidate.SetCv(key, command.FileName, command.ContentType, command.Length, uploadedAt);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CvDto(command.FileName, command.ContentType, command.Length, uploadedAt);
    }
}

/// <summary>Handles <see cref="GetCandidateCvMetadataQuery"/>.</summary>
/// <param name="repository">The candidate repository.</param>
public sealed class GetCandidateCvMetadataQueryHandler(ICandidateRepository repository)
    : IQueryHandler<GetCandidateCvMetadataQuery, CvDto>
{
    private readonly ICandidateRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<CvDto>> HandleAsync(GetCandidateCvMetadataQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var candidate = await _repository.GetByIdAsync(new CandidateId(query.CandidateId), cancellationToken).ConfigureAwait(false);
        if (candidate is null || !candidate.HasCv)
        {
            return Error.NotFound("cv.not_found", "No CV has been uploaded for this candidate.");
        }

        return new CvDto(candidate.CvFileName!, candidate.CvContentType!, candidate.CvSize, candidate.CvUploadedAt!.Value);
    }
}

/// <summary>Handles <see cref="DownloadCandidateCvQuery"/>.</summary>
/// <param name="repository">The candidate repository.</param>
/// <param name="storage">Object storage.</param>
public sealed class DownloadCandidateCvQueryHandler(ICandidateRepository repository, IObjectStorage storage)
    : IQueryHandler<DownloadCandidateCvQuery, CvContent>
{
    private readonly ICandidateRepository _repository = repository;
    private readonly IObjectStorage _storage = storage;

    /// <inheritdoc />
    public async Task<Result<CvContent>> HandleAsync(DownloadCandidateCvQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var candidate = await _repository.GetByIdAsync(new CandidateId(query.CandidateId), cancellationToken).ConfigureAwait(false);
        if (candidate is null || !candidate.HasCv)
        {
            return Error.NotFound("cv.not_found", "No CV has been uploaded for this candidate.");
        }

        var download = await _storage.GetAsync(CvStorage.Bucket, candidate.CvObjectKey!, cancellationToken).ConfigureAwait(false);
        if (download is null)
        {
            return Error.NotFound("cv.not_found", "The stored CV could not be found.");
        }

        return new CvContent(download.Content, download.ContentType, candidate.CvFileName ?? "cv");
    }
}
