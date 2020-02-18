using System;

namespace Sitko.Core.Storage.S3
{
    public interface IS3StorageOptions
    {
        Uri Server { get; }
        string Bucket { get; }
        string AccessKey { get; }
        string SecretKey { get; }
    }
}
