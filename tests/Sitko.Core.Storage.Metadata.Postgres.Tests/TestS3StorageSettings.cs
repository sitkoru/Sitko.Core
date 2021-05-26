using System;
using Amazon;
using Sitko.Core.Storage.S3;

namespace Sitko.Core.Storage.Metadata.Postgres.Tests
{
    public class TestS3StorageSettings : S3StorageOptions
    {
        public override string Name { get; set; } = "test_s3_storage";
    }
}
