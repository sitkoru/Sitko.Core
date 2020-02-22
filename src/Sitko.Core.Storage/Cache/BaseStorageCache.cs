using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Storage.Cache
{
    public abstract class BaseStorageCache<TOptions, TRecord> : IStorageCache<TOptions>
        where TOptions : StorageCacheOptions where TRecord : StorageCacheRecord
    {
        protected readonly TOptions Options;
        protected readonly ILogger<BaseStorageCache<TOptions, TRecord>> Logger;
        private readonly Dictionary<string, StorageItem> _items = new Dictionary<string, StorageItem>();

        private IMemoryCache? _cache;
        private ConcurrentDictionary<object, SemaphoreSlim> _locks = new ConcurrentDictionary<object, SemaphoreSlim>();

        protected BaseStorageCache(TOptions options, ILogger<BaseStorageCache<TOptions, TRecord>> logger)
        {
            Options = options;
            Logger = logger;
            _cache = new MemoryCache(new MemoryCacheOptions {SizeLimit = Options.MaxCacheSize});
        }

        public IEnumerator<StorageItem> GetEnumerator()
        {
            return _items.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public async Task<StorageRecord?> GetOrAddItemAsync(string path,
            Func<Task<StorageRecord?>> addItem)
        {
            var key = NormalizePath(path);
            if (!_cache.TryGetValue(key, out TRecord? cacheEntry)) // Look for cache key.
            {
                var itemLock = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));

                await itemLock.WaitAsync();
                try
                {
                    if (!_cache.TryGetValue(key, out cacheEntry))
                    {
                        var result = await addItem();
                        if (result == null)
                        {
                            throw new Exception($"File {key} not found");
                        }

                        var storageRecord = result.Value;

                        if (Options.MaxFileSizeToStore > 0 &&
                            storageRecord.StorageItem.FileSize > Options.MaxFileSizeToStore)
                        {
                            Logger.LogWarning(
                                "File {Key} exceed maximum cache file size. File size: {FleSize}. Maximum size: {MaximumSize}",
                                key, storageRecord.StorageItem.HumanSize,
                                Helpers.HumanSize(Options.MaxFileSizeToStore));
                            return result.Value;
                        }

                        if (storageRecord.Stream == null)
                        {
                            throw new Exception($"Stream for file {key} is empty!");
                        }

#if NETSTANDARD2_1
                        await using (storageRecord.Stream)
#else
                                using (storageRecord.Stream)
#endif
                        {
                            Logger.LogDebug("Download file {Key}", key);
                            cacheEntry = await GetEntryAsync(storageRecord.StorageItem, storageRecord.Stream);

                            var options = new MemoryCacheEntryOptions {SlidingExpiration = Options.Ttl};
                            options.RegisterPostEvictionCallback((objKey, value, reason, state) =>
                            {
                                if (value is TRecord deletedRecord)
                                {
                                    DisposeItem(deletedRecord);
                                }


                                _items.Remove(objKey.ToString());
                                Logger.LogDebug("Remove file {ObjKey} from cache", key);
                            });
                            if (Options.MaxCacheSize > 0)
                            {
                                options.Size = storageRecord.StorageItem.FileSize;
                            }

                            Logger.LogDebug("Add file {Key} to cache", key);
                            _cache.Set(key, cacheEntry, options);
                            _items.Add(key, storageRecord.StorageItem);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.ToString());
                    return null;
                }
                finally
                {
                    itemLock.Release();
                }
            }

            return await GetStorageRecord(cacheEntry);
        }


        protected abstract void DisposeItem(TRecord deletedRecord);

        protected abstract Task<TRecord> GetEntryAsync(StorageItem item, Stream stream);

        private string NormalizePath(string path)
        {
            path = new Uri(path, UriKind.Relative).ToString();
            if (!path.StartsWith("/"))
            {
                path = $"/{path}";
            }

            return path;
        }

        protected abstract Task<StorageRecord> GetStorageRecord(TRecord record);

        public async Task<StorageRecord?> GetItemAsync(string path)
        {
            var record = _cache.Get<TRecord?>(NormalizePath(path));
            if (record == null)
            {
                return null;
            }

            return await GetStorageRecord(record);
        }

        public Task RemoveItemAsync(string path)
        {
            _cache.Remove(NormalizePath(path));
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _cache?.Dispose();
            _cache = new MemoryCache(new MemoryCacheOptions {SizeLimit = Options.MaxCacheSize});
            Logger.LogDebug("Cache cleared");
            return Task.CompletedTask;
        }

        public abstract ValueTask DisposeAsync();
    }
}
