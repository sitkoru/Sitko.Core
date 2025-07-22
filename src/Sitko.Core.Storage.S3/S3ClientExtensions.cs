using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Storage.S3;

public static class S3ClientExtensions
{
    internal static async Task<bool> IsObjectExistsAsync(this AmazonS3Client client, string bucket, string filePath,
        CancellationToken cancellationToken = default)
    {
        var request = new ListObjectsRequest { BucketName = bucket, Prefix = filePath, MaxKeys = 1 };

        var response = await client.ListObjectsAsync(request, cancellationToken);

        return response.S3Objects?.Count > 0;
    }

    internal static async Task<GetObjectResponse?> DownloadFileAsync(this AmazonS3Client client, string bucket,
        string path, ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var request = new GetObjectRequest { BucketName = bucket, Key = path };
        GetObjectResponse? response = null;
        try
        {
            response = await client.GetObjectAsync(request, cancellationToken);
        }
        catch (AmazonS3Exception ex)
        {
            if (string.Equals(ex.ErrorCode, "NoSuchBucket", StringComparison.Ordinal))
            {
                throw;
            }

            if (string.Equals(ex.ErrorCode, "NotFound", StringComparison.Ordinal) ||
                string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.Ordinal))
            {
                logger.LogDebug(ex, "File {File} not found", path);
            }
        }

        return response;
    }

    internal static async Task<string> DownloadStreamAsString(this Stream stream,
        CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    internal static AmazonS3Config GetAmazonS3Config<TS3StorageOptions>(this TS3StorageOptions s3StorageOptions,
        S3HttpClientFactory<TS3StorageOptions> httpClientFactory) where TS3StorageOptions : S3StorageOptions, new()
    {
        var config = new AmazonS3Config
        {
            RegionEndpoint = s3StorageOptions.Region,
            ServiceURL = s3StorageOptions.Server!.ToString(),
            ForcePathStyle = true,
            HttpClientFactory = httpClientFactory
        };
        return config;
    }
}
