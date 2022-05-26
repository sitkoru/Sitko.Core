using Amazon.S3;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Storage.S3
{
    public class S3ClientProvider<TS3StorageOptions> where TS3StorageOptions : S3StorageOptions, new()
    {
        private readonly IOptionsMonitor<TS3StorageOptions> optionsMonitor;

        public AmazonS3Client S3Client
        {
            get
            {
                var config = new AmazonS3Config
                {
                    RegionEndpoint = optionsMonitor.CurrentValue.Region,
                    ServiceURL = optionsMonitor.CurrentValue.Server!.ToString(),
                    ForcePathStyle = true
                };
                return new AmazonS3Client(optionsMonitor.CurrentValue.AccessKey,
                    optionsMonitor.CurrentValue.SecretKey, config);
            }
        }

        public S3ClientProvider(IOptionsMonitor<TS3StorageOptions> optionsMonitor) =>
            this.optionsMonitor = optionsMonitor;
    }
}
