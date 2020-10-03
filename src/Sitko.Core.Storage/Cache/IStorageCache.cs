using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitko.Core.Storage.Cache
{
    public interface IStorageCache : IEnumerable<StorageItem>, IAsyncDisposable
    {
        Task<StorageItem?> GetItemAsync(string path);

        Task<StorageItem?> GetOrAddItemAsync(string path, Func<Task<StorageItem?>> addItem);

        Task RemoveItemAsync(string path);
        Task ClearAsync();
    }

    // ReSharper disable once UnusedTypeParameter
    public interface IStorageCache<T> : IStorageCache where T : StorageCacheOptions
    {
    }

    public abstract class StorageCacheOptions
    {
        public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(12);
        public long MaxFileSizeToStore { get; set; }
        public long? MaxCacheSize { get; set; }
    }
}
