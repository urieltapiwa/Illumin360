using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Illumin360.Storage;

/// <summary>Wires the S3-compatible object store (MinIO in dev) into DI.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="IObjectStorage"/> backed by an S3 client configured from the <c>Storage</c>
    /// configuration section (falling back to the MinIO dev defaults).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">App configuration (reads the <c>Storage</c> section).</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddIllumin360Storage(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new StorageOptions();
        configuration.GetSection("Storage").Bind(options);

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var config = new AmazonS3Config
            {
                ServiceURL = options.Endpoint,
                ForcePathStyle = true, // MinIO uses path-style bucket addressing
                AuthenticationRegion = options.Region,

                // AWS SDK v4 defaults to sending flexible (CRC) checksums, which some MinIO builds reject;
                // only add them when the operation actually requires it.
                RequestChecksumCalculation = Amazon.Runtime.RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = Amazon.Runtime.ResponseChecksumValidation.WHEN_REQUIRED,
            };
            return new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
        });

        services.AddScoped<IObjectStorage, S3ObjectStorage>();
        return services;
    }
}
