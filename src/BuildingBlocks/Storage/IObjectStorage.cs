namespace Illumin360.Storage;

/// <summary>A downloaded object's content stream and metadata.</summary>
/// <param name="Content">The object's content stream (caller disposes).</param>
/// <param name="ContentType">The stored MIME type.</param>
public sealed record ObjectDownload(Stream Content, string ContentType);

/// <summary>
/// Port for S3-compatible object storage (MinIO in dev). Services store uploaded files (CVs, documents)
/// and read them back through this abstraction rather than binding to a specific client.
/// </summary>
public interface IObjectStorage
{
    /// <summary>Stores an object, creating the bucket on first use.</summary>
    /// <param name="bucket">Bucket name.</param>
    /// <param name="key">Object key.</param>
    /// <param name="content">Content stream.</param>
    /// <param name="contentType">MIME type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PutAsync(string bucket, string key, Stream content, string contentType, CancellationToken cancellationToken);

    /// <summary>Reads an object back, or <see langword="null"/> if it does not exist.</summary>
    /// <param name="bucket">Bucket name.</param>
    /// <param name="key">Object key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The object's content + type, or null.</returns>
    Task<ObjectDownload?> GetAsync(string bucket, string key, CancellationToken cancellationToken);
}
