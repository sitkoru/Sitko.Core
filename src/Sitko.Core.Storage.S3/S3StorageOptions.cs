using System;

namespace Sitko.Core.Storage.S3
{
    public abstract class S3StorageOptions : StorageOptions
    {
        public Uri Server { get; }
        public string Bucket { get; }
        public string AccessKey { get; }
        public string SecretKey { get; }

        protected S3StorageOptions(Uri publicUri, Uri server, string bucket, string accessKey, string secretKey) :
            base(publicUri)
        {
            Server = server;
            Bucket = bucket;
            AccessKey = accessKey;
            SecretKey = secretKey;
        }
    }
}
