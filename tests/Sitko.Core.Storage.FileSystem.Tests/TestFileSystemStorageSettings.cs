using System;
using System.Collections.Generic;
using System.Linq;

namespace Sitko.Core.Storage.FileSystem.Tests
{
    public class TestFileSystemStorageSettings : IFileSystemStorageOptions
    {
        public Uri PublicUri { get; }
        public List<StorageImageSize> Thumbnails { get; } = new List<StorageImageSize>();

        public TestFileSystemStorageSettings(Uri publicUri, string storagePath, IReadOnlyCollection<StorageImageSize> thumbnails = null)
        {
            PublicUri = publicUri;
            StoragePath = storagePath;
            if (thumbnails != null && thumbnails.Any())
            {
                Thumbnails.AddRange(thumbnails);
            }
        }

        public string StoragePath { get; }
    }
}
