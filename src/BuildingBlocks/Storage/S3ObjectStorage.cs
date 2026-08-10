using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace Illumin360.Storage;

/// <summary>S3-compatible implementation of <see cref="IObjectStorage"/> (MinIO in dev).</summary>
/// <param name="client">The S3 client.</param>
public sealed class S3ObjectStorage(IAmazonS3 client) : IObjectStorage
{
    private readonly IAmazonS3 _client = client;

    /// <inheritdoc />
    public async Task PutAsync(string bucket, string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        if (!await AmazonS3Util.DoesS3BucketExistV2Async(_client, bucket).ConfigureAwait(false))
        {
            await _client.PutBucketAsync(new PutBucketRequest { BucketName = bucket }, cancellationToken).ConfigureAwait(false);
        }

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
        };

        await _client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ObjectDownload?> GetAsync(string bucket, string key, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.GetObjectAsync(bucket, key, cancellationToken).ConfigureAwait(false);
            return new ObjectDownload(response.ResponseStream, response.Headers.ContentType ?? "application/octet-stream");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
