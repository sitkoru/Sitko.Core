using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Storage.Cache;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.S3
{
    public sealed class S3Storage<TStorageOptions> : Storage<TStorageOptions>
        where TStorageOptions : S3StorageOptions, new()
    {
        private readonly AmazonS3Client _s3Client;

        public S3Storage(IOptionsMonitor<TStorageOptions> options, S3ClientProvider<TStorageOptions> s3ClientProvider,
            ILogger<S3Storage<TStorageOptions>> logger,
            IStorageCache<TStorageOptions>? cache = null,
            IStorageMetadataProvider<TStorageOptions>? metadataProvider = null) : base(options, logger,
            cache, metadataProvider)
        {
            _s3Client = s3ClientProvider.S3Client;
        }

        private async Task CreateBucketAsync(string bucketName, CancellationToken? cancellationToken = null)
        {
            try
            {
                var bucketExists = await IsBucketExistsAsync(bucketName);
                if (!bucketExists)
                {
                    var putBucketRequest = new PutBucketRequest {BucketName = bucketName, UseClientRegion = true};

                    await _s3Client.PutBucketAsync(putBucketRequest, cancellationToken ?? CancellationToken.None);
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
            return AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);
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
            using var fileTransferUtility = new TransferUtility(_s3Client);
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
            return _s3Client.IsObjectExistsAsync(Options.Bucket, filePath, cancellationToken);
        }

        internal Task DeleteObjectAsync(string filePath, CancellationToken? cancellationToken = null)
        {
            return _s3Client.DeleteObjectAsync(Options.Bucket, filePath,
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
            return _s3Client.GetObjectMetadataAsync(request, cancellationToken ?? CancellationToken.None);
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
                if (string.IsNullOrEmpty(Options.Prefix))
                {
                    try
                    {
                        await AmazonS3Util.DeleteS3BucketWithObjectsAsync(_s3Client, Options.Bucket,
                            cancellationToken ?? CancellationToken.None);
                    }
                    catch (AmazonS3Exception ex) when (ex.Message.Contains(
                        "A header you provided implies functionality that is not implemented"))
                    {
                        await DeleteAllObjectsInBucket(cancellationToken);
                        await _s3Client.DeleteBucketAsync(Options.Bucket);
                    }
                }
                else
                {
                    await DeleteAllObjectsInBucket(cancellationToken);
                }
            }
        }

        private async Task DeleteAllObjectsInBucket(CancellationToken? cancellationToken = null)
        {
            var objects = await GetAllItemsAsync("/");
            foreach (var chunk in SplitList(objects.ToList(), 1000))
            {
                var request = new DeleteObjectsRequest
                {
                    BucketName = Options.Bucket,
                    Objects = chunk.Select(item => new KeyVersion {Key = GetPathWithPrefix(item.Path)})
                        .ToList()
                };
                await _s3Client.DeleteObjectsAsync(request, cancellationToken ?? CancellationToken.None);
            }
        }

        public static IEnumerable<List<TItem>> SplitList<TItem>(List<TItem> locations, int nSize = 30)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }

        internal Task<GetObjectResponse?> DownloadFileAsync(string filePath,
            CancellationToken? cancellationToken = null)
        {
            return _s3Client.DownloadFileAsync(Options.Bucket, filePath, Logger,
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
                    response = await _s3Client.ListObjectsV2Async(request, cancellationToken ?? CancellationToken.None);
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
            _s3Client.Dispose();
            return base.DisposeAsync();
        }
    }
}
