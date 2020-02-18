using System;

namespace Sitko.Core.Storage.S3.Tests
{
    public class TestS3StorageSettings : StorageOptions, IS3StorageOptions
    {
        public Uri Server { get; }
        public string Bucket { get; }
        public string AccessKey { get; }
        public string SecretKey { get; }

        public TestS3StorageSettings(Uri publicUri, Uri server, string bucket, string accessKey, string secretKey)
        {
            PublicUri = publicUri;
            Server = server;
            Bucket = bucket;
            AccessKey = accessKey;
            SecretKey = secretKey;
        }
    }
}
