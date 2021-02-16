using System;
using Amazon;
using Sitko.Core.Storage.S3;

namespace Sitko.Core.Storage.Metadata.Postgres.Tests
{
    public class TestS3StorageSettings : StorageOptions, IS3StorageOptions
    {
        public Uri Server { get; set; }
        public string Bucket { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public RegionEndpoint Region { get; set; } = RegionEndpoint.USEast1;
        public override string Name { get; set; } = "test_s3_storage";
    }
}
