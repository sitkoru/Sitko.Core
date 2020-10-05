using System;
using System.IO;

namespace Sitko.Core.Storage
{
    public class FileDownloadResult
    {
        public Stream Stream { get; }

        public StorageItemMetadata Metadata { get; }

        public long FileSize { get; }

        public string? PhysicalPath { get; }

        public FileDownloadResult(StorageItemMetadata metadata, long fileSize, Stream stream,
            string? physicalPath = null)
        {
            Metadata = metadata;
            FileSize = fileSize;
            Stream = stream;
            PhysicalPath = physicalPath;
        }
    }
}
