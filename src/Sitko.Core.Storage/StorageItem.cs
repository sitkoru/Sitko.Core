using System;
using System.IO;

namespace Sitko.Core.Storage
{
    public class StorageItem
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string FilePath { get; set; }
        public string Path { get; set; }
        public string StorageFileName => FilePath.Substring(FilePath.LastIndexOf('/') + 1);

        public string HumanSize
        {
            get
            {
                return Helpers.HumanSize(FileSize);
            }
        }

        public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;
    }

    public struct StorageRecord
    {
        public StorageRecord(StorageItem storageItem, string path)
        {
            StorageItem = storageItem;
            Path = path;
            Stream = null;
            LastModified = storageItem.LastModified;
        }

        public StorageRecord(StorageItem storageItem, Stream stream)
        {
            StorageItem = storageItem;
            Path = null;
            Stream = stream;
            LastModified = storageItem.LastModified;
        }

        public StorageItem StorageItem { get; }
        public string? Path { get; }
        public Stream? Stream { get; }
        public DateTimeOffset LastModified { get; }
    }
}
