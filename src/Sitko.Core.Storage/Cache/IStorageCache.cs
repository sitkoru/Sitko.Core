using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sitko.Core.App;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Cache
{
    // Generic parameter is required for dependency injection
    // ReSharper disable once UnusedTypeParameter
    public interface IStorageCache<TStorageOptions> : IAsyncDisposable where TStorageOptions : StorageOptions
    {
        internal Task<StorageItemDownloadInfo?> GetItemAsync(string path,
            CancellationToken cancellationToken = default);

        internal Task<StorageItemDownloadInfo?> GetOrAddItemAsync(string path,
            Func<Task<StorageItemDownloadInfo?>> addItem,
            CancellationToken cancellationToken = default);

        Task RemoveItemAsync(string path, CancellationToken cancellationToken = default);
        Task ClearAsync(CancellationToken cancellationToken = default);
    }

    // Generic interface is required for dependency injection
    // ReSharper disable once UnusedTypeParameter
    public interface IStorageCache<TStorageOptions, TCacheOptions> : IStorageCache<TStorageOptions>
        where TCacheOptions : StorageCacheOptions where TStorageOptions : StorageOptions
    {
    }

    public interface IStorageCacheRecord
    {
        StorageItemMetadata? Metadata { get; }

        long FileSize { get; }
        DateTimeOffset Date { get; }

        public Stream OpenRead();
    }

    public abstract class StorageCacheOptions : BaseModuleOptions
    {
        public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(12);
        public long MaxFileSizeToStore { get; set; }
        public long? MaxCacheSize { get; set; }
    }
}
