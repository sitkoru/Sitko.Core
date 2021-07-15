using Amazon.S3;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Storage.S3
{
    public class S3ClientProvider<TS3StorageOptions> where TS3StorageOptions : S3StorageOptions, new()
    {
        private readonly IOptionsMonitor<TS3StorageOptions> optionsMonitor;
        private AmazonS3Client? s3Client;

        public AmazonS3Client S3Client
        {
            get
            {
                if (s3Client is not null)
                {
                    return s3Client;
                }

                var config = new AmazonS3Config
                {
                    RegionEndpoint = optionsMonitor.CurrentValue.Region,
                    ServiceURL = optionsMonitor.CurrentValue.Server!.ToString(),
                    ForcePathStyle = true
                };
                s3Client = new AmazonS3Client(optionsMonitor.CurrentValue.AccessKey,
                    optionsMonitor.CurrentValue.SecretKey, config);

                return s3Client;
            }
        }

        public S3ClientProvider(IOptionsMonitor<TS3StorageOptions> optionsMonitor) =>
            this.optionsMonitor = optionsMonitor;
    }
}
