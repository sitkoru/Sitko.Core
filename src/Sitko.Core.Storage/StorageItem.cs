using System;
using System.IO;
using System.Threading.Tasks;

namespace Sitko.Core.Storage
{
    public sealed class StorageItem : IStorageNode, IAsyncDisposable
    {
        public Stream? Stream { get; internal set; }

        public string? FileName { get; set; }
        public long FileSize { get; set; }
        public string? FilePath { get; set; }
        public string Name => FileName;
        public string FullPath => FilePath;
        public DateTimeOffset LastModified { get; set; }
        public string? PhysicalPath { get; internal set; }

        public string Path { get; set; }
        public string? StorageFileName => FilePath?.Substring(FilePath.LastIndexOf('/') + 1);

        internal StorageItemMetadata? Metadata { get; set; }

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

        public TMetadata? GetMetadata<TMetadata>() where TMetadata : class
        {
            return Metadata?.GetData<TMetadata>();
        }

        public string HumanSize
        {
            get
            {
                return Helpers.HumanSize(FileSize);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Stream != null)
            {
                await Stream.DisposeAsync();
            }
        }
    }
}
