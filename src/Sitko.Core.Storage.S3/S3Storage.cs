using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Storage.S3
{
    public sealed class S3Storage<T> : Storage<T>, IDisposable where T : S3StorageOptions
    {
        private readonly ILogger<S3Storage<T>> _logger;
        private readonly T _options;
        private readonly AmazonS3Client _client;
        private bool _disposed;

        public S3Storage(T options, ILogger<S3Storage<T>> logger) : base(options, logger)
        {
            _logger = logger;
            _options = options;

            var config = new AmazonS3Config
            {
                RegionEndpoint =
                    RegionEndpoint
                        .USEast1, // MUST set this before setting ServiceURL and it should match the `MINIO_REGION` enviroment variable.
                ServiceURL = _options.Server.ToString(), // replace http://localhost:9000 with URL of your minio server
                ForcePathStyle = true // MUST be true to work correctly with Minio server
            };
            _client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _client.Dispose();
        }

        private async Task CreateBucketAsync(string bucketName)
        {
            try
            {
                var bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_client, bucketName);
                if (!bucketExists)
                {
                    var putBucketRequest = new PutBucketRequest {BucketName = bucketName, UseClientRegion = true};

                    await _client.PutBucketAsync(putBucketRequest);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        protected override async Task<bool> DoSaveAsync(string path, Stream file)
        {
            await CreateBucketAsync(_options.Bucket);
            using var fileTransferUtility = new TransferUtility(_client);
            try
            {
                await fileTransferUtility.UploadAsync(file,
                    _options.Bucket, path);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public override async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                await _client.DeleteObjectAsync(_options.Bucket, filePath);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
