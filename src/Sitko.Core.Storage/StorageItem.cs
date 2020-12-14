using System;

namespace Sitko.Core.Storage
{
    public sealed class StorageItem
    {
        public string? FileName { get; set; }
        public long FileSize { get; set; }
        public string? FilePath { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public string Path { get; set; }

        internal StorageItemMetadata? Metadata { get; set; }

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
    }
}
