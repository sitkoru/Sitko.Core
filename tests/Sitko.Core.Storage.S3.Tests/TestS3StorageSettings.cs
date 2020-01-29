using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;

namespace Sitko.Core.Storage.S3.Tests
{
    public class TestS3StorageSettings : IS3StorageOptions
    {
        public Uri PublicUri { get; }
        public List<StorageImageSize> Thumbnails { get; } = new List<StorageImageSize>();
        public Uri Server { get; }
        public string Bucket { get; }
        public string AccessKey { get; }
        public string SecretKey { get; }

        public TestS3StorageSettings(Uri publicUri, Uri server, string bucket, string accessKey, string secretKey,
            IReadOnlyCollection<StorageImageSize> thumbnails = null)
        {
            PublicUri = publicUri;
            Server = server;
            Bucket = bucket;
            AccessKey = accessKey;
            SecretKey = secretKey;
            if (thumbnails != null && thumbnails.Any())
            {
                Thumbnails.AddRange(thumbnails);
            }
        }
    }
}
