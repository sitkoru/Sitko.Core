using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;
using Sitko.Core.Storage.Cache;

namespace Sitko.Core.Storage.S3
{
    public sealed class S3Storage<T> : Storage<T> where T : StorageOptions, IS3StorageOptions, new()
    {
        private readonly AmazonS3Client _s3Client;

        public S3Storage(T options, S3ClientProvider<T> s3ClientProvider, ILogger<S3Storage<T>> logger,
            IStorageCache? cache = null)
            : base(options, logger,
                cache)
        {
            _s3Client = s3ClientProvider.S3Client;
        }

        private async Task CreateBucketAsync(string bucketName)
        {
            try
            {
                var bucketExists = await IsBucketExists(bucketName);
                if (!bucketExists)
                {
                    var putBucketRequest = new PutBucketRequest {BucketName = bucketName, UseClientRegion = true};

                    await _s3Client.PutBucketAsync(putBucketRequest);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating bucket {Bucket}: {ErrorText}", bucketName, ex.ToString());
                throw;
            }
        }

        private Task<bool> IsBucketExists(string bucketName)
        {
            return AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
        }

        private async Task<bool> IsObjectExists(string filePath)
        {
            var request = new ListObjectsRequest {BucketName = Options.Bucket, Prefix = filePath, MaxKeys = 1};

            var response = await _s3Client.ListObjectsAsync(request);

            return response.S3Objects.Any();
        }

        protected override async Task<bool> DoSaveAsync(string path, Stream file,
            string metadata)
        {
            await CreateBucketAsync(Options.Bucket);
            using var fileTransferUtility = new TransferUtility(_s3Client);
            try
            {
                var request = new TransferUtilityUploadRequest
                {
                    InputStream = file, Key = path, BucketName = Options.Bucket
                };
                await fileTransferUtility.UploadAsync(request);

                var metaDataRequest = new TransferUtilityUploadRequest
                {
                    InputStream = new MemoryStream(Encoding.UTF8.GetBytes(metadata)),
                    Key = GetMetaDataPath(path),
                    BucketName = Options.Bucket
                };
                await fileTransferUtility.UploadAsync(metaDataRequest);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error uploading file {File}: {ErrorText}", path, e.ToString());
                throw;
            }
        }

        protected override async Task<bool> DoDeleteAsync(string filePath)
        {
            if (await IsBucketExists(Options.Bucket))
            {
                if (await IsObjectExists(filePath))
                {
                    try
                    {
                        await _s3Client.DeleteObjectAsync(Options.Bucket, filePath);
                        if (await IsObjectExists(GetMetaDataPath(filePath)))
                        {
                            await _s3Client.DeleteObjectAsync(Options.Bucket, GetMetaDataPath(filePath));
                        }

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

        private Task<GetObjectMetadataResponse> GetFileMetadata(StorageItem item)
        {
            var request = new GetObjectMetadataRequest {BucketName = Options.Bucket, Key = item.FilePath};
            return _s3Client.GetObjectMetadataAsync(request);
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
            if (await IsBucketExists(Options.Bucket))
            {
                await AmazonS3Util.DeleteS3BucketWithObjectsAsync(_s3Client, Options.Bucket);
            }
        }

        private async Task<GetObjectResponse?> DownloadFileAsync(string path)
        {
            var request = new GetObjectRequest {BucketName = Options.Bucket, Key = path};
            GetObjectResponse? response = null;
            try
            {
                response = await _s3Client.GetObjectAsync(request);
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
                metaData = await DownloadStreamAsString(metaDataResponse.ResponseStream);
            }

            return metaData;
        }

        private async Task<string> DownloadStreamAsString(Stream stream)
        {
            await using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            return Encoding.UTF8.GetString(buffer.ToArray());
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
                var request = new ListObjectsV2Request {BucketName = Options.Bucket, Prefix = Options.Prefix};
                ListObjectsV2Response response;
                var objects = new Dictionary<string, S3Object>();
                do
                {
                    Logger.LogDebug("Get objects list from S3. Current objects count: {Count}", objects.Count);
                    response = await _s3Client.ListObjectsV2Async(request);
                    foreach (var s3Object in response.S3Objects)
                    {
                        objects.Add(s3Object.Key, s3Object);
                    }

                    request.ContinuationToken = response.NextContinuationToken;
                } while (response.IsTruncated);

                foreach (var s3Object in objects.Values)
                {
                    await AddObjectAsync(s3Object, root, objects);
                }
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

        private async Task AddObjectAsync(S3Object s3Object, StorageNode root, Dictionary<string, S3Object> s3Objects)
        {
            if (s3Object.Key.EndsWith(MetaDataExtension)) return;

            string? metadata = null;
            var metadataPath = GetMetaDataPath(s3Object.Key);
            if (s3Objects.ContainsKey(metadataPath))
            {
                metadata = await DownloadFileMetadataAsync(s3Object.Key);
            }

            var item = CreateStorageItem(s3Object.Key, s3Object.LastModified, s3Object.Size, metadata);

            root.AddItem(item);
        }

        public override ValueTask DisposeAsync()
        {
            _s3Client.Dispose();
            return base.DisposeAsync();
        }
    }
}
