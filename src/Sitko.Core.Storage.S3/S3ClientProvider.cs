using Amazon.S3;

namespace Sitko.Core.Storage.S3
{
    public class S3ClientProvider<TS3StorageOptions> where TS3StorageOptions : StorageOptions, IS3StorageOptions, new()
    {
        public AmazonS3Client S3Client { get; }

        public S3ClientProvider(AmazonS3Client s3Client)
        {
            S3Client = s3Client;
        }
    }
}
