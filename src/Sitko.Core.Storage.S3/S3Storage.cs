using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;
using Sitko.Core.Storage.Cache;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.S3
{
    public sealed class S3Storage<T> : Storage<T> where T : StorageOptions, IS3StorageOptions
    {
        private readonly AmazonS3Client _client;

        public S3Storage(T options, ILogger<S3Storage<T>> logger, IStorageCache? cache = null,
            IStorageMetadataProvider? metadataProvider = null) : base(options, logger,
            cache, metadataProvider)
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.USEast1,
                ServiceURL = Options.Server.ToString(),
                ForcePathStyle = true
            };
            _client = new AmazonS3Client(Options.AccessKey, Options.SecretKey, config);
        }

        private async Task CreateBucketAsync(string bucketName, CancellationToken? cancellationToken = null)
        {
            try
            {
                var bucketExists = await IsBucketExistsAsync(bucketName);
                if (!bucketExists)
                {
                    var putBucketRequest = new PutBucketRequest {BucketName = bucketName, UseClientRegion = true};

                    await _client.PutBucketAsync(putBucketRequest, cancellationToken ?? CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating bucket {Bucket}: {ErrorText}", bucketName, ex.ToString());
                throw;
            }
        }

        private Task<bool> IsBucketExistsAsync(string bucketName)
        {
            return AmazonS3Util.DoesS3BucketExistV2Async(_client, bucketName);
        }

        internal Task<bool> DoSaveInternalAsync(string path, Stream file,
            CancellationToken? cancellationToken = null)
        {
            return DoSaveAsync(path, file, cancellationToken);
        }

        protected override async Task<bool> DoSaveAsync(string path, Stream file,
            CancellationToken? cancellationToken = null)
        {
            await CreateBucketAsync(Options.Bucket, cancellationToken);
            using var fileTransferUtility = new TransferUtility(_client);
            try
            {
                var request = new TransferUtilityUploadRequest
                {
                    InputStream = file, Key = path, BucketName = Options.Bucket
                };
                await fileTransferUtility.UploadAsync(request, cancellationToken ?? CancellationToken.None);

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error uploading file {File}: {ErrorText}", path, e.ToString());
                throw;
            }
        }

        internal Task<bool> IsObjectExistsAsync(string filePath, CancellationToken? cancellationToken = null)
        {
            return _client.IsObjectExistsAsync(Options.Bucket, filePath, cancellationToken);
        }

        internal Task DeleteObjectAsync(string filePath, CancellationToken? cancellationToken = null)
        {
            return _client.DeleteObjectAsync(Options.Bucket, filePath,
                cancellationToken ?? CancellationToken.None);
        }

        protected override async Task<bool> DoDeleteAsync(string filePath, CancellationToken? cancellationToken = null)
        {
            if (await IsBucketExistsAsync(Options.Bucket))
            {
                if (await IsObjectExistsAsync(filePath, cancellationToken))
                {
                    try
                    {
                        await DeleteObjectAsync(filePath, cancellationToken);

                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Error deleting file {File}: {ErrorText}", filePath, e.ToString());
                    }
                }
            }

            return false;
        }

        private Task<GetObjectMetadataResponse> GetFileMetadataAsync(StorageItem item,
            CancellationToken? cancellationToken = null)
        {
            var request = new GetObjectMetadataRequest {BucketName = Options.Bucket, Key = item.FilePath};
            return _client.GetObjectMetadataAsync(request, cancellationToken ?? CancellationToken.None);
        }

        protected override async Task<bool> DoIsFileExistsAsync(StorageItem item,
            CancellationToken? cancellationToken = null)
        {
            try
            {
                await GetFileMetadataAsync(item, cancellationToken);
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

        protected override async Task DoDeleteAllAsync(CancellationToken? cancellationToken = null)
        {
            if (await IsBucketExistsAsync(Options.Bucket))
            {
                await AmazonS3Util.DeleteS3BucketWithObjectsAsync(_client, Options.Bucket,
                    cancellationToken ?? CancellationToken.None);
            }
        }

        internal Task<GetObjectResponse?> DownloadFileAsync(string filePath,
            CancellationToken? cancellationToken = null)
        {
            return _client.DownloadFileAsync(Options.Bucket, filePath, Logger,
                cancellationToken ?? CancellationToken.None);
        }

        internal override async Task<StorageItemDownloadInfo?> DoGetFileAsync(string path,
            CancellationToken? cancellationToken = null)
        {
            var fileResponse = await DownloadFileAsync(path, cancellationToken ?? CancellationToken.None);
            if (fileResponse == null)
            {
                return null;
            }

            return new StorageItemDownloadInfo(fileResponse.ContentLength, fileResponse.LastModified,
                () => fileResponse.ResponseStream);
        }

        internal override async Task<IEnumerable<StorageItemInfo>> GetAllItemsAsync(string path,
            CancellationToken? cancellationToken = null)
        {
            var items = new List<StorageItemInfo>();
            try
            {
                var request = new ListObjectsV2Request {BucketName = Options.Bucket, Prefix = GetPathWithPrefix(path)};
                ListObjectsV2Response response;
                do
                {
                    Logger.LogDebug("Get objects list from S3. Current objects count: {Count}", items.Count);
                    response = await _client.ListObjectsV2Async(request, cancellationToken ?? CancellationToken.None);
                    items.AddRange(response.S3Objects.Select(s3Object =>
                        new StorageItemInfo(s3Object.Key, s3Object.Size, s3Object.LastModified)));

                    request.ContinuationToken = response.NextContinuationToken;
                } while (response.IsTruncated);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                Logger.LogError(amazonS3Exception, "S3 error occurred. Exception: {ErrorText}",
                    amazonS3Exception.ToString());
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception. Exception: {ErrorText}",
                    e.ToString());
            }

            return items;
        }

        public override ValueTask DisposeAsync()
        {
            _client.Dispose();
            return base.DisposeAsync();
        }
    }
}
