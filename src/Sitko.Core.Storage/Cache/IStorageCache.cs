using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Cache
{
    public interface IStorageCache : IAsyncDisposable
    {
        internal Task<StorageItemDownloadInfo?> GetItemAsync(string path, CancellationToken? cancellationToken = default);

        internal Task<StorageItemDownloadInfo?> GetOrAddItemAsync(string path, Func<Task<StorageItemDownloadInfo?>> addItem,
            CancellationToken? cancellationToken = default);

        Task RemoveItemAsync(string path, CancellationToken? cancellationToken = default);
        Task ClearAsync(CancellationToken? cancellationToken = default);
    }

    // Generic interface is required for dependency injection
    // ReSharper disable once UnusedTypeParameter
    public interface IStorageCache<T> : IStorageCache where T : StorageCacheOptions
    {
    }

    public interface IStorageCacheRecord
    {
        StorageItemMetadata? Metadata { get; }

        long FileSize { get; }
        DateTimeOffset Date { get; }

        public Stream OpenRead();
    }

    public abstract class StorageCacheOptions
    {
        public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(12);
        public long MaxFileSizeToStore { get; set; }
        public long? MaxCacheSize { get; set; }
    }
}
