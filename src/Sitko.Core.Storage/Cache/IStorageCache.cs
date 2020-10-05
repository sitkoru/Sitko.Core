using System;
using System.IO;
using System.Threading.Tasks;

namespace Sitko.Core.Storage.Cache
{
    public interface IStorageCache : IAsyncDisposable
    {
        Task<FileDownloadResult?> GetItemAsync(string path);

        Task<FileDownloadResult?> GetOrAddItemAsync(string path, Func<Task<FileDownloadResult?>> addItem);

        Task RemoveItemAsync(string path);
        Task ClearAsync();
    }

    // ReSharper disable once UnusedTypeParameter
    public interface IStorageCache<T> : IStorageCache where T : StorageCacheOptions
    {
    }

    public interface IStorageCacheRecord
    {
        StorageItemMetadata Metadata { get; }

        long FileSize { get; }

        public Stream OpenRead();
        public string? PhysicalPath { get; }
    }

    public abstract class StorageCacheOptions
    {
        public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(12);
        public long MaxFileSizeToStore { get; set; }
        public long? MaxCacheSize { get; set; }
    }
}
