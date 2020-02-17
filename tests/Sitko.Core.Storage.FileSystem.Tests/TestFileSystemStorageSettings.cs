using System;

namespace Sitko.Core.Storage.FileSystem.Tests
{
    public class TestFileSystemStorageSettings : StorageOptions, IFileSystemStorageOptions
    {
        public TestFileSystemStorageSettings(Uri publicUri, string storagePath)
        {
            PublicUri = publicUri;
            StoragePath = storagePath;
        }

        public string StoragePath { get; }
    }
}
