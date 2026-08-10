namespace Illumin360.Storage;

/// <summary>Configuration for the S3-compatible object store (bound from the <c>Storage</c> section).</summary>
public sealed class StorageOptions
{
    /// <summary>Service endpoint URL (e.g. <c>http://minio:9000</c> in-cluster).</summary>
    public string Endpoint { get; set; } = "http://minio:9000";

    /// <summary>Access key (MinIO root user in dev).</summary>
    public string AccessKey { get; set; } = "illumin";

    /// <summary>Secret key (MinIO root password in dev).</summary>
    public string SecretKey { get; set; } = "illumin12345";

    /// <summary>Signing region (MinIO ignores it, but the SDK requires one).</summary>
    public string Region { get; set; } = "us-east-1";
}
