using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sitko.Core.Storage.Cache
{
    public interface IStorageCache: IEnumerable<StorageItem>
    {
        Task<StorageItem?> GetItemAsync(string path);
        Task<Stream?> GetItemStreamAsync(string path);
        Task<StorageItem?> GetOrAddItemAsync(string path, Func<Task<StorageItem>> addItem);

        Task<Stream?> GetOrAddItemStreamAsync(string path, Func<Task<(StorageItem item, Stream stream)?>> addItem);

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
