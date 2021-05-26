using Amazon.S3;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Storage.S3
{
    public class S3ClientProvider<TS3StorageOptions> where TS3StorageOptions : S3StorageOptions, new()
    {
        private readonly IOptionsMonitor<TS3StorageOptions> _optionsMonitor;
        private AmazonS3Client? _s3Client;

        public AmazonS3Client S3Client
        {
            get
            {
                if (_s3Client is null)
                {
                    var config = new AmazonS3Config
                    {
                        RegionEndpoint = _optionsMonitor.CurrentValue.Region,
                        ServiceURL = _optionsMonitor.CurrentValue.Server!.ToString(),
                        ForcePathStyle = true
                    };
                    _s3Client = new AmazonS3Client(_optionsMonitor.CurrentValue.AccessKey,
                        _optionsMonitor.CurrentValue.SecretKey, config);
                }

                return _s3Client;
            }
        }

        public S3ClientProvider(IOptionsMonitor<TS3StorageOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
        }
    }
}
