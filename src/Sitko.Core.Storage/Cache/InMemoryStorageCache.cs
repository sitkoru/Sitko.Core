using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Storage.Cache
{
    public class InMemoryStorageCache : BaseStorageCache<InMemoryStorageCacheOptions, InMemoryStorageCacheRecord>
    {
        public InMemoryStorageCache(InMemoryStorageCacheOptions options, ILogger<InMemoryStorageCache> logger) : base(
            options, logger)
        {
        }

        protected override void DisposeItem(InMemoryStorageCacheRecord deletedRecord)
        {
        }

        protected override Task<InMemoryStorageCacheRecord> GetEntryAsync(FileDownloadResult item, Stream stream)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return Task.FromResult(
                new InMemoryStorageCacheRecord(item.Metadata, item.FileSize, item.Date, memoryStream.ToArray()));
        }

        public override ValueTask DisposeAsync()
        {
            return new ValueTask();
        }
    }

    public class InMemoryStorageCacheOptions : StorageCacheOptions
    {
    }

    public class InMemoryStorageCacheRecord : IStorageCacheRecord
    {
        private byte[] Data { get; }
        public string? Metadata { get; }
        public long FileSize { get; }
        public DateTimeOffset Date { get; }
        public string? PhysicalPath { get; }

        public InMemoryStorageCacheRecord(string? metadata, long fileSize, DateTimeOffset date, byte[] data)
        {
            Metadata = metadata;
            FileSize = fileSize;
            Data = data;
            Date = date;
        }

        public Stream OpenRead()
        {
            return new MemoryStream(Data);
        }
    }
}
