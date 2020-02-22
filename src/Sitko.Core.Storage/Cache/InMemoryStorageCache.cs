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

        protected override Task<InMemoryStorageCacheRecord> GetEntryAsync(StorageItem item, Stream stream)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return Task.FromResult(new InMemoryStorageCacheRecord(item, memoryStream.ToArray()));
        }

        protected override Task<StorageRecord> GetStorageRecord(InMemoryStorageCacheRecord record)
        {
            return Task.FromResult(new StorageRecord(record.Item, record.GetStream()));
        }

        public override ValueTask DisposeAsync()
        {
            return new ValueTask();
        }
    }

    public class InMemoryStorageCacheOptions : StorageCacheOptions
    {
    }

    public class InMemoryStorageCacheRecord : StorageCacheRecord
    {
        public InMemoryStorageCacheRecord(StorageItem item, byte[] data) : base(item)
        {
            Data = data;
        }

        public byte[] Data { get; }

        public Stream GetStream()
        {
            return new MemoryStream(Data);
        }
    }
}
