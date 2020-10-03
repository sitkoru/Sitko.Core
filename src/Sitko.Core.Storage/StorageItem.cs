using System;
using System.IO;

namespace Sitko.Core.Storage
{
    public class StorageItem : IStorageNode
    {
        private readonly Stream? _stream;

        public StorageItem()
        {
        }

        public StorageItem(StorageItem item)
        {
            FileName = item.FileName;
            FileSize = item.FileSize;
            FilePath = item.FilePath;
            Path = item.Path;
        }

        public StorageItem(Stream stream)
        {
            _stream = stream;
        }

        public string? FileName { get; set; }
        public long FileSize { get; set; }
        public string? FilePath { get; set; }
        public string Name => FileName;
        public string FullPath => FilePath;
        public virtual DateTimeOffset LastModified { get; set; }
        public virtual string? PhysicalPath { get; protected set; }

        public virtual Stream? OpenRead()
        {
            return _stream;
        }

        public string Path { get; set; }
        public string? StorageFileName => FilePath?.Substring(FilePath.LastIndexOf('/') + 1);

        public string HumanSize
        {
            get
            {
                return Helpers.HumanSize(FileSize);
            }
        }
    }
}
