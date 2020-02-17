using System;
using System.Threading.Tasks;

namespace Sitko.Core.Storage.Cache
{
    public interface IStorageCache
    {
        Task<StorageItem?> GetItemAsync(string path);
        Task<bool> AddItemAsync(string path, StorageItem item);
        Task<bool> IsKeyExistsAsync(string path);
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
    }
}
