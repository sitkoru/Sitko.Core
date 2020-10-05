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

        protected override async Task<bool> DoSaveAsync(string path, Stream file,
            StorageItemMetadata metadata)
        {
            await CreateBucketAsync(_options.Bucket);
            using var fileTransferUtility = new TransferUtility(_client);
            try
            {
                var request = new TransferUtilityUploadRequest
                {
                    InputStream = file, Key = path, BucketName = _options.Bucket
                };
                foreach (var metaDataEntry in metadata.Metadata)
                {
                    request.Metadata.Add(metaDataEntry.Key, metaDataEntry.Value);
                }

                await fileTransferUtility.UploadAsync(request);
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

        protected override async Task<FileDownloadResult?> DoGetFileAsync(string path)
        {
            var request = new GetObjectRequest {BucketName = _options.Bucket, Key = path};

            try
            {
                var response = await _client.GetObjectAsync(request);
                var metaData = new StorageItemMetadata()
                    .Add(StorageItemMetadata.FieldFileName, Path.GetFileName(path))
                    .Add(StorageItemMetadata.FieldDate, response.LastModified.ToString("O"));
                foreach (var key in response.Metadata.Keys)
                {
                    metaData.Add(key.Replace("x-amz-meta-", ""), response.Metadata[key]);
                }

                return new FileDownloadResult(metaData, response.ContentLength, response.ResponseStream);
            }
            catch (AmazonS3Exception ex)
            {
                if (string.Equals(ex.ErrorCode, "NoSuchBucket"))
                {
                    throw;
                }

                if (string.Equals(ex.ErrorCode, "NotFound"))
                {
                    Logger.LogDebug(ex, "File {File} not found", path);
                }
            }

            return null;
        }

        protected override async Task<StorageFolder?> DoBuildStorageTreeAsync()
        {
            var root = new StorageFolder("/", "/");
            try
            {
                ListObjectsV2Request request = new ListObjectsV2Request {BucketName = _options.Bucket};
                ListObjectsV2Response response;
                do
                {
                    response = await _client.ListObjectsV2Async(request);
                    foreach (var s3Object in response.S3Objects)
                    {
                        AddObject(s3Object, root);
                    }

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

            return root;
        }

        private void AddObject(S3Object s3Object, StorageFolder root)
        {
            var parts = s3Object.Key.Split("/");
            var current = root;
            foreach (var part in parts)
            {
                if (part == parts.Last())
                {
                    current.AddChild(GetStorageItem(s3Object));
                }
                else
                {
                    var child = current.Children.OfType<StorageFolder>().FirstOrDefault(f => f.Name == part);
                    if (child == null)
                    {
                        child = new StorageFolder(part, PreparePath(Path.Combine(current.FullPath, part)));
                        current.AddChild(child);
                    }

                    current = child;
                }
            }
        }

        private StorageItem GetStorageItem(S3Object s3Object)
        {
            return new StorageItem
            {
                FileName = Path.GetFileName(s3Object.Key),
                FileSize = s3Object.Size,
                Path = Path.GetDirectoryName(s3Object.Key),
                FilePath = s3Object.Key,
                LastModified = s3Object.LastModified
            };
        }

        public override ValueTask DisposeAsync()
        {
            _client.Dispose();
            return base.DisposeAsync();
        }
    }
}
