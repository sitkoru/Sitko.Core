using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using CacheExtensions = Sitko.Core.Caching.CacheExtensions;
using MemoryCache = Sitko.Core.Caching.MemoryCache;
using MemoryCacheOptions = Sitko.Core.Caching.MemoryCacheOptions;

namespace Sitko.Core.Storage.Cache
{
    public abstract class BaseStorageCache<TOptions, TRecord> : IStorageCache<TOptions>
        where TOptions : StorageCacheOptions where TRecord : StorageRecord
    {
        protected readonly TOptions Options;
        protected readonly ILogger<BaseStorageCache<TOptions, TRecord>> Logger;

        private MemoryCache? _cache;

        private readonly ConcurrentDictionary<object, SemaphoreSlim> _locks =
            new ConcurrentDictionary<object, SemaphoreSlim>();

        protected BaseStorageCache(TOptions options, ILogger<BaseStorageCache<TOptions, TRecord>> logger)
        {
            Options = options;
            Logger = logger;
            _cache = new MemoryCache(new MemoryCacheOptions {SizeLimit = Options.MaxCacheSize});
        }

        protected void Expire()
        {
            _cache.Expire();
        }

        public IEnumerator<StorageRecord> GetEnumerator()
        {
            return _cache.Values<StorageRecord>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public async Task<StorageRecord?> GetOrAddItemAsync(string path,
            Func<Task<StorageRecord?>> addItem)
        {
            var key = NormalizePath(path);
            if (!CacheExtensions.TryGetValue(_cache, key, out TRecord? cacheEntry)) // Look for cache key.
            {
                var itemLock = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));

                await itemLock.WaitAsync();
                try
                {
                    if (!CacheExtensions.TryGetValue(_cache, key, out cacheEntry))
                    {
                        var result = await addItem();
                        if (result == null)
                        {
                            throw new Exception($"File {key} not found");
                        }

                        if (Options.MaxFileSizeToStore > 0 &&
                            result.FileSize > Options.MaxFileSizeToStore)
                        {
                            Logger.LogWarning(
                                "File {Key} exceed maximum cache file size. File size: {FleSize}. Maximum size: {MaximumSize}",
                                key, result.HumanSize,
                                Helpers.HumanSize(Options.MaxFileSizeToStore));
                            return result;
                        }

                        var stream = result.OpenRead();
                        if (stream == null)
                        {
                            throw new Exception($"Stream for file {key} is empty!");
                        }

                        await using (stream)
                        {
                            Logger.LogDebug("Download file {Key}", key);
                            cacheEntry = await GetEntryAsync(result, stream);

                            var options = new MemoryCacheEntryOptions {SlidingExpiration = Options.Ttl};
                            options.RegisterPostEvictionCallback((objKey, value, reason, state) =>
                            {
                                if (value is TRecord deletedRecord)
                                {
                                    DisposeItem(deletedRecord);
                                }

                                Logger.LogDebug("Remove file {ObjKey} from cache", key);
                            });
                            if (Options.MaxCacheSize > 0)
                            {
                                options.Size = result.FileSize;
                            }

                            Logger.LogDebug("Add file {Key} to cache", key);
                            _cache.Set(key, cacheEntry, options);
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

            return cacheEntry;
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

        public Task<StorageRecord?> GetItemAsync(string path)
        {
            var record = CacheExtensions.Get<TRecord?>(_cache, NormalizePath(path));

            return Task.FromResult<StorageRecord>(record);
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
