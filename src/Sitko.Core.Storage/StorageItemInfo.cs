using System;

namespace Sitko.Core.Storage
{
    internal readonly struct StorageItemInfo
    {
        public string Path { get; }
        public long FileSize { get; }
        public DateTimeOffset Date { get; }

        public StorageItemInfo(string path, long fileSize, DateTimeOffset date)
        {
            Path = path;
            FileSize = fileSize;
            Date = date;
        }
    }
}
