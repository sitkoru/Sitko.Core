using System;
using System.IO;

namespace Sitko.Core.Storage
{
    public class StorageItemInfo
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
