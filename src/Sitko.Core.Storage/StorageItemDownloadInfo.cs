using System;
using System.IO;
using System.Runtime.CompilerServices;
using Sitko.Core.Storage.Metadata;

[assembly: InternalsVisibleTo("Sitko.Core.Storage.S3")]
[assembly: InternalsVisibleTo("Sitko.Core.Storage.FileSystem")]

namespace Sitko.Core.Storage
{
    internal struct StorageItemDownloadInfo
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
            Metadata = null;
        }

        public void SetMetadata(StorageItemMetadata metadata)
        {
            Metadata = metadata;
        }
    }
}
