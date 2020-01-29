using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Storage.S3
{
    public sealed class S3Storage<T> : Storage<T> where T : IS3StorageOptions
    {
        private readonly T _options;
        private readonly AmazonS3Client _client;
        private bool _disposed;
        private readonly List<FileStream> _openedStreams = new List<FileStream>();

        public S3Storage(T options, ILogger<S3Storage<T>> logger) : base(options, logger)
        {
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

        public override async Task<bool> DeleteFileAsync(string filePath)
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

        public override async Task<Stream> DownloadFileAsync(StorageItem item)
        {
            var path = Path.GetTempFileName();
            using (var utility = new TransferUtility(_client))
            {
                await utility.DownloadAsync(path, _options.Bucket, item.FilePath);
            }

            var stream = File.OpenRead(path);
            _openedStreams.Add(stream);
            return stream;
        }

        public override async Task DeleteAllAsync()
        {
            if (await IsBucketExists(_options.Bucket))
            {
                await AmazonS3Util.DeleteS3BucketWithObjectsAsync(_client, _options.Bucket);
            }
        }

        private void ClearStreams()
        {
            if (_openedStreams.Any())
            {
                foreach (FileStream stream in _openedStreams)
                {
                    stream.Close();
                }

                _openedStreams.Clear();
            }
        }

        public override ValueTask DisposeAsync()
        {
            _client.Dispose();
            ClearStreams();
            return base.DisposeAsync();
        }
    }
}
