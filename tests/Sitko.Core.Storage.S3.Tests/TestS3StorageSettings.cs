using System;
using Amazon;

namespace Sitko.Core.Storage.S3.Tests
{
    public class TestS3StorageSettings : StorageOptions, IS3StorageOptions
    {
        public Uri Server { get; set; }
        public string Bucket { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public RegionEndpoint Region { get; } = RegionEndpoint.USEast1;
    }
}
