using Illumin360.SharedKernel;
using Illumin360.Storage;
using Illumin360.Students.Application.Abstractions;

namespace Illumin360.Students.Application.Students;

/// <summary>Metadata describing the current ("me") student's uploaded CV.</summary>
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

/// <summary>Uploads (or replaces) the current ("me") student's CV.</summary>
/// <param name="FileName">Original file name.</param>
/// <param name="ContentType">MIME type.</param>
/// <param name="Length">Size in bytes.</param>
/// <param name="Content">Content stream.</param>
public sealed record UploadCvCommand(string FileName, string ContentType, long Length, Stream Content) : ICommand<CvDto>;

/// <summary>Reads the current ("me") student's CV metadata.</summary>
public sealed record GetCvMetadataQuery : IQuery<CvDto>;

/// <summary>Downloads the current ("me") student's CV content.</summary>
public sealed record DownloadCvQuery : IQuery<CvContent>;

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

/// <summary>Handles <see cref="UploadCvCommand"/>.</summary>
/// <param name="repository">The student repository.</param>
/// <param name="storage">Object storage.</param>
public sealed class UploadCvCommandHandler(IStudentRepository repository, IObjectStorage storage)
    : ICommandHandler<UploadCvCommand, CvDto>
{
    private readonly IStudentRepository _repository = repository;
    private readonly IObjectStorage _storage = storage;

    /// <inheritdoc />
    public async Task<Result<CvDto>> HandleAsync(UploadCvCommand command, CancellationToken cancellationToken)
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

        var meId = await _repository.GetDefaultStudentIdAsync(cancellationToken).ConfigureAwait(false);
        if (meId is not { } id)
        {
            return Error.NotFound("student.not_found", "No student profile found.");
        }

        var me = await _repository.GetTrackedAsync(id, cancellationToken).ConfigureAwait(false);
        if (me is null)
        {
            return Error.NotFound("student.not_found", "No student profile found.");
        }

        var ext = command.ContentType switch
        {
            "application/pdf" => ".pdf",
            "application/msword" => ".doc",
            _ => ".docx",
        };
        var key = $"students/{id.Value}/cv{ext}";

        await _storage.PutAsync(CvStorage.Bucket, key, command.Content, command.ContentType, cancellationToken).ConfigureAwait(false);

        var uploadedAt = DateTimeOffset.UtcNow;
        me.SetCv(key, command.FileName, command.ContentType, command.Length, uploadedAt);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CvDto(command.FileName, command.ContentType, command.Length, uploadedAt);
    }
}

/// <summary>Handles <see cref="GetCvMetadataQuery"/>.</summary>
/// <param name="repository">The student repository.</param>
public sealed class GetCvMetadataQueryHandler(IStudentRepository repository)
    : IQueryHandler<GetCvMetadataQuery, CvDto>
{
    private readonly IStudentRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<CvDto>> HandleAsync(GetCvMetadataQuery query, CancellationToken cancellationToken)
    {
        var me = await ResolveMeAsync(_repository, cancellationToken).ConfigureAwait(false);
        if (me is null || !me.HasCv)
        {
            return Error.NotFound("cv.not_found", "No CV has been uploaded.");
        }

        return new CvDto(me.CvFileName!, me.CvContentType!, me.CvSize, me.CvUploadedAt!.Value);
    }

    /// <summary>Resolves the default ("me") student (tracked), or null if none exist.</summary>
    /// <param name="repository">The student repository.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The student, or null.</returns>
    internal static async Task<Domain.Student?> ResolveMeAsync(IStudentRepository repository, CancellationToken ct)
    {
        var meId = await repository.GetDefaultStudentIdAsync(ct).ConfigureAwait(false);
        return meId is { } id ? await repository.GetTrackedAsync(id, ct).ConfigureAwait(false) : null;
    }
}

/// <summary>Handles <see cref="DownloadCvQuery"/>.</summary>
/// <param name="repository">The student repository.</param>
/// <param name="storage">Object storage.</param>
public sealed class DownloadCvQueryHandler(IStudentRepository repository, IObjectStorage storage)
    : IQueryHandler<DownloadCvQuery, CvContent>
{
    private readonly IStudentRepository _repository = repository;
    private readonly IObjectStorage _storage = storage;

    /// <inheritdoc />
    public async Task<Result<CvContent>> HandleAsync(DownloadCvQuery query, CancellationToken cancellationToken)
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

        return new CvContent(download.Content, download.ContentType, me.CvFileName ?? "cv");
    }
}
