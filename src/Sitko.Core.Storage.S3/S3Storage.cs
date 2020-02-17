using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;
using Sitko.Core.Storage.Cache;

namespace Sitko.Core.Storage.S3
{
    public sealed class S3Storage<T> : Storage<T> where T : StorageOptions, IS3StorageOptions
    {
        private readonly T _options;
        private readonly AmazonS3Client _client;

        public S3Storage(T options, ILogger<S3Storage<T>> logger, IStorageCache? cache = null) : base(options, logger,
            cache)
        {
            _options = options;

            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.USEast1,
                ServiceURL = _options.Server.ToString(),
                ForcePathStyle = true
            };
            _client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
        }

        private async Task CreateBucketAsync(string bucketName)
        {
            try
            {
                var bucketExists = await IsBucketExists(bucketName);
                if (!bucketExists)
                {
                    var putBucketRequest = new PutBucketRequest {BucketName = bucketName, UseClientRegion = true};

                    await _client.PutBucketAsync(putBucketRequest);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        private Task<bool> IsBucketExists(string bucketName)
        {
            return AmazonS3Util.DoesS3BucketExistV2Async(_client, bucketName);
        }

        private async Task<bool> IsObjectExists(string filePath)
        {
            var request = new ListObjectsRequest {BucketName = _options.Bucket, Prefix = filePath, MaxKeys = 1};

            var response = await _client.ListObjectsAsync(request);

            return response.S3Objects.Any();
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
                Logger.LogError(e, e.Message);
                throw;
            }
        }

        protected override async Task<bool> DoDeleteAsync(string filePath)
        {
            if (await IsBucketExists(_options.Bucket))
            {
                if (await IsObjectExists(filePath))
                {
                    try
                    {
                        await _client.DeleteObjectAsync(_options.Bucket, filePath);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, e.Message);
                    }
                }
            }

            return false;
        }

        private Task<GetObjectMetadataResponse> GetFileMetadata(StorageItem item)
        {
            var request = new GetObjectMetadataRequest {BucketName = _options.Bucket, Key = item.FilePath};
            return _client.GetObjectMetadataAsync(request);
        }

        protected override async Task<bool> DoIsFileExistsAsync(StorageItem item)
        {
            try
            {
                await GetFileMetadata(item);
                return true;
            }
            catch (AmazonS3Exception e)
            {
                if (string.Equals(e.ErrorCode, "NoSuchBucket"))
                {
                    return false;
                }

                if (string.Equals(e.ErrorCode, "NotFound"))
                {
                    return false;
                }

                throw;
            }
        }

        protected override async Task DoDeleteAllAsync()
        {
            if (await IsBucketExists(_options.Bucket))
            {
                await AmazonS3Util.DeleteS3BucketWithObjectsAsync(_client, _options.Bucket);
            }
        }

        protected override async Task<StorageItem> DoGetFileInfoAsync(StorageItem item)
        {
            var request = new GetObjectRequest {BucketName = _options.Bucket, Key = item.FilePath};

            var response = await _client.GetObjectAsync(request);

            item = new StorageItem
            {
                Path = Path.GetDirectoryName(item.FilePath),
                FileName = Path.GetFileName(item.FilePath),
                FilePath = item.FilePath,
                FileSize = response.ContentLength,
                LastModified = response.LastModified
            };
            item.SetStream(response.ResponseStream);
            return item;
        }

        public override async Task<StorageItemCollection> GetDirectoryContentsAsync(string path)
        {
            var request = new ListObjectsRequest {BucketName = _options.Bucket, Prefix = path};

            var response = await _client.ListObjectsAsync(request);

            var files = response.S3Objects.Select(entry => new StorageItem
            {
                FileSize = entry.Size,
                FileName = Path.GetFileName(entry.Key),
                LastModified = entry.LastModified,
                FilePath = entry.Key,
                Path = Path.GetDirectoryName(entry.Key)
            }).ToList();

            return new StorageItemCollection(files);
        }

        public override ValueTask DisposeAsync()
        {
            _client.Dispose();
            return base.DisposeAsync();
        }
    }
}
