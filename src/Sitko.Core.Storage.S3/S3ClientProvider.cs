using Amazon.S3;

namespace Sitko.Core.Storage.S3
{
    public class S3ClientProvider<TS3StorageOptions> where TS3StorageOptions : StorageOptions, IS3StorageOptions, new()
    {
        private readonly TS3StorageOptions _storageOptions;
        private AmazonS3Client? _s3Client;

        public AmazonS3Client S3Client
        {
            get
            {
                if (_s3Client is null)
                {
                    var config = new AmazonS3Config
                    {
                        RegionEndpoint = _storageOptions.Region,
                        ServiceURL = _storageOptions.Server.ToString(),
                        ForcePathStyle = true
                    };
                    _s3Client = new AmazonS3Client(_storageOptions.AccessKey, _storageOptions.SecretKey, config);
                }

                return _s3Client;
            }
        }

        public S3ClientProvider(TS3StorageOptions storageOptions)
        {
            _storageOptions = storageOptions;
        }
    }
}
