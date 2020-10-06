using System;
using System.IO;

namespace Sitko.Core.Storage
{
    public class FileDownloadResult
    {
        public Stream Stream { get; }

        public string? Metadata { get; }

        public long FileSize { get; }
        public DateTimeOffset Date { get; }

        public string? PhysicalPath { get; }

        public FileDownloadResult(string? metadata, long fileSize, DateTimeOffset date, Stream stream,
            string? physicalPath = null)
        {
            Metadata = metadata;
            FileSize = fileSize;
            Date = date;
            Stream = stream;
            PhysicalPath = physicalPath;
        }
    }
}
