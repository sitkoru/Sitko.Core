using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Storage.Internal;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Cache;

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
        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return
            new InMemoryStorageCacheRecord(item.Metadata, item.FileSize, item.Date, memoryStream.ToArray());
    }

    public override ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return new ValueTask();
    }
}

public class InMemoryStorageCacheOptions : StorageCacheOptions
{
}

public class InMemoryStorageCacheRecord : IStorageCacheRecord
{
    public InMemoryStorageCacheRecord(StorageItemMetadata? metadata, long fileSize, DateTimeOffset date,
        byte[] data)
    {
        Metadata = metadata;
        FileSize = fileSize;
        Data = data;
        Date = date;
    }

    private byte[] Data { get; }
    public StorageItemMetadata? Metadata { get; }
    public long FileSize { get; }
    public DateTimeOffset Date { get; }

    public Stream OpenRead() => new MemoryStream(Data);
}

