using Amazon.S3;
using Amazon.S3.Model;
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
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exist = await DoesS3BucketExistAsync(cancellationToken);
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

    private async Task<bool> DoesS3BucketExistAsync(CancellationToken cancellationToken)
    {
        try
        {
            var s3Client = s3ClientProvider.GetS3Client<TS3StorageOptions>();
            await s3Client.GetBucketAclAsync(new GetBucketAclRequest { BucketName = options.CurrentValue.Bucket },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (AmazonS3Exception e)
        {
            switch (e.ErrorCode)
            {
                // A redirect error or a forbidden error means the bucket exists.
                case "AccessDenied":
                case "PermanentRedirect":
                    return true;
                case "NoSuchBucket":
                    return false;
                default:
                    throw;
            }
        }

        return true;
    }
}
