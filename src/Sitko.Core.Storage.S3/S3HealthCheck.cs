using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Storage.S3;

public class S3HealthCheck<TS3StorageOptions>(
    S3ClientProvider s3ClientProvider,
    IOptionsMonitor<TS3StorageOptions> options,
    ILogger<S3BucketHealthCheck<TS3StorageOptions>> logger)
    : IHealthCheck where TS3StorageOptions : S3StorageOptions, new()
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var s3Client = s3ClientProvider.GetS3Client<TS3StorageOptions>();
            var listRequest = new ListObjectsRequest { BucketName = options.CurrentValue.Bucket, MaxKeys = 1 };
            await s3Client.ListObjectsAsync(listRequest, cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while checking s3 objects: {ErrorText}", ex.Message);
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
