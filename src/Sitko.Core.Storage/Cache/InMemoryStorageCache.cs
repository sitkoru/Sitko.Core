using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Cache
{
    public class InMemoryStorageCache<TStorageOptions> : BaseStorageCache<TStorageOptions, InMemoryStorageCacheOptions,
        InMemoryStorageCacheRecord> where TStorageOptions : StorageOptions
    {
        public InMemoryStorageCache(IOptionsMonitor<InMemoryStorageCacheOptions> options,
            ILogger<InMemoryStorageCache<TStorageOptions>> logger) : base(
            options, logger)
        {
        }

        protected override void DisposeItem(InMemoryStorageCacheRecord deletedRecord)
        {
        }

        internal override async Task<InMemoryStorageCacheRecord> GetEntryAsync(StorageItemDownloadInfo item,
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            return
                new InMemoryStorageCacheRecord(item.Metadata, item.FileSize, item.Date, memoryStream.ToArray());
        }

        public override ValueTask DisposeAsync()
        {
            return new();
        }
    }

    public class InMemoryStorageCacheOptions : StorageCacheOptions
    {
    }

    public class InMemoryStorageCacheRecord : IStorageCacheRecord
    {
        private byte[] Data { get; }
        public StorageItemMetadata? Metadata { get; }
        public long FileSize { get; }
        public DateTimeOffset Date { get; }

        public InMemoryStorageCacheRecord(StorageItemMetadata? metadata, long fileSize, DateTimeOffset date,
            byte[] data)
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
