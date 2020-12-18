using System;
using System.IO;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Sitko.Core.Storage.S3")]
[assembly: InternalsVisibleTo("Sitko.Core.Storage.FileSystem")]
namespace Sitko.Core.Storage
{
    internal class StorageItemInfo
    {
        public Func<Stream> GetStream { get; }

        public string? Metadata { get; }

        public long FileSize { get; }
        public DateTimeOffset Date { get; }

        public StorageItemInfo(string? metadata, long fileSize, DateTimeOffset date, Func<Stream> getStream)
        {
            Metadata = metadata;
            FileSize = fileSize;
            Date = date;
            GetStream = getStream;
        }
    }
}
