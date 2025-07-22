using Amazon.S3.Util;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Storage.S3;

public class S3BucketHealthCheck<TS3StorageOptions>(
    S3ClientProvider s3ClientProvider,
    IOptionsMonitor<TS3StorageOptions> options,
    ILogger<S3BucketHealthCheck<TS3StorageOptions>> logger)
    : IHealthCheck where TS3StorageOptions : S3StorageOptions, new()
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var s3Client = s3ClientProvider.GetS3Client<TS3StorageOptions>();
            var exist = await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, options.CurrentValue.Bucket);
            return !exist
                ? HealthCheckResult.Unhealthy($"Bucket {options.CurrentValue.Bucket} does not exist")
                : HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while checking s3 bucket: {ErrorText}", ex.Message);
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
