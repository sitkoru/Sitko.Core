using System;
using System.IO;
using System.Linq;
using System.Text;
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
            string metadata)
        {
            await CreateBucketAsync(_options.Bucket);
            using var fileTransferUtility = new TransferUtility(_client);
            try
            {
                var request = new TransferUtilityUploadRequest
                {
                    InputStream = file, Key = path, BucketName = _options.Bucket
                };
                await fileTransferUtility.UploadAsync(request);

                var metaDataRequest = new TransferUtilityUploadRequest
                {
                    InputStream = new MemoryStream(Encoding.UTF8.GetBytes(metadata)),
                    Key = GetMetaDataPath(path),
                    BucketName = _options.Bucket
                };
                await fileTransferUtility.UploadAsync(metaDataRequest);
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
                        if (await IsObjectExists(GetMetaDataPath(filePath)))
                        {
                            await _client.DeleteObjectAsync(_options.Bucket, GetMetaDataPath(filePath));
                        }

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

        private async Task<GetObjectResponse?> DownloadFileAsync(string path)
        {
            var request = new GetObjectRequest {BucketName = _options.Bucket, Key = path};
            GetObjectResponse? response = null;
            try
            {
                response = await _client.GetObjectAsync(request);
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

            return response;
        }

        private async Task<string?> DownloadFileMetadataAsync(string filePath)
        {
            string? metaData = null;
            var metaDataResponse = await DownloadFileAsync(GetMetaDataPath(filePath));
            if (metaDataResponse != null)
            {
                var buffer = new MemoryStream();
                await metaDataResponse.ResponseStream.CopyToAsync(buffer);
                metaData = Encoding.UTF8.GetString(buffer.ToArray());
            }

            return metaData;
        }

        internal override async Task<StorageItemInfo?> DoGetFileAsync(string path)
        {
            var fileResponse = await DownloadFileAsync(path);
            if (fileResponse == null)
            {
                return null;
            }

            var metaData = await DownloadFileMetadataAsync(path);

            return new StorageItemInfo(metaData, fileResponse.ContentLength, fileResponse.LastModified,
                () => fileResponse.ResponseStream);
        }

        protected override async Task<StorageNode?> DoBuildStorageTreeAsync()
        {
            var root = StorageNode.CreateDirectory("/", "/");
            try
            {
                ListObjectsV2Request request = new ListObjectsV2Request {BucketName = _options.Bucket};
                ListObjectsV2Response response;
                do
                {
                    response = await _client.ListObjectsV2Async(request);
                    foreach (var s3Object in response.S3Objects)
                    {
                        await AddObjectAsync(s3Object, root);
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

        private async Task AddObjectAsync(S3Object s3Object, StorageNode root)
        {
            if (s3Object.Key.EndsWith(MetaDataExtension)) return;
            var parts = s3Object.Key.Split("/");
            var current = root;
            foreach (var part in parts)
            {
                if (part == parts.Last())
                {
                    var metadata = await DownloadFileMetadataAsync(s3Object.Key);
                    var item = CreateStorageItem(s3Object.Key, s3Object.LastModified, s3Object.Size, metadata);
                    current.AddChild(StorageNode.CreateStorageItem(item));
                }
                else
                {
                    var child = current.Children.Where(n => n.Type == StorageNodeType.Directory)
                        .FirstOrDefault(f => f.Name == part);
                    if (child == null)
                    {
                        child = StorageNode.CreateDirectory(part, PreparePath(Path.Combine(current.FullPath, part)));
                        current.AddChild(child);
                    }

                    current = child;
                }
            }
        }

        public override ValueTask DisposeAsync()
        {
            _client.Dispose();
            return base.DisposeAsync();
        }
    }
}
