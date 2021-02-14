using System;
using System.IO;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Sitko.Core.Storage.S3")]
[assembly: InternalsVisibleTo("Sitko.Core.Storage.FileSystem")]

namespace Sitko.Core.Storage
{
    internal class StorageItemDownloadInfo
    {
        public Func<Stream> GetStream { get; }

        public StorageItemMetadata? Metadata { get; private set; }

        public long FileSize { get; }
        public DateTimeOffset Date { get; }

        public StorageItemDownloadInfo(long fileSize, DateTimeOffset date, Func<Stream> getStream)
        {
            FileSize = fileSize;
            Date = date;
            GetStream = getStream;
        }

        public void SetMetadata(StorageItemMetadata metadata)
        {
            Metadata = metadata;
        }
    }

    internal struct StorageItemInfo
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
