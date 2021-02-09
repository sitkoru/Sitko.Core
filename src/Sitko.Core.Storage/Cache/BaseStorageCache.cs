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
    public abstract class BaseStorageCache<TOptions, TRecord> : IStorageCache<TOptions>, IEnumerable<TRecord>
        where TOptions : StorageCacheOptions where TRecord : class, IStorageCacheRecord
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
            _cache?.Expire();
        }

        public IEnumerator<TRecord> GetEnumerator()
        {
            if (_cache == null)
            {
                throw new Exception("Cache is not initialized");
            }

            return _cache.Values<TRecord>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        async Task<StorageItemInfo?> IStorageCache.GetOrAddItemAsync(string path,
            Func<Task<StorageItemInfo?>> addItem)
        {
            if (_cache == null)
            {
                throw new Exception("Cache is not initialized");
            }

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
                            Logger.LogDebug("File {File} not found", key);
                            return null;
                        }

                        if (Options.MaxFileSizeToStore > 0 &&
                            result.FileSize > Options.MaxFileSizeToStore)
                        {
                            Logger.LogWarning(
                                "File {Key} exceed maximum cache file size. File size: {FleSize}. Maximum size: {MaximumSize}",
                                key, result.FileSize,
                                Helpers.HumanSize(Options.MaxFileSizeToStore));
                            return result;
                        }

                        var stream = result.GetStream();

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
                    Logger.LogError(ex, "Error caching file {File}: {ErrorText}", key, ex.ToString());
                    return null;
                }
                finally
                {
                    itemLock.Release();
                }
            }

            return cacheEntry is null
                ? null
                : new StorageItemInfo(cacheEntry.Metadata, cacheEntry.FileSize, cacheEntry.Date,
                    () => cacheEntry.OpenRead());
        }


        protected abstract void DisposeItem(TRecord deletedRecord);

        internal abstract Task<TRecord> GetEntryAsync(StorageItemInfo item, Stream stream);

        private string NormalizePath(string path)
        {
            path = new Uri(path, UriKind.Relative).ToString();
            if (!path.StartsWith("/"))
            {
                path = $"/{path}";
            }

            return path;
        }

        Task<StorageItemInfo?> IStorageCache.GetItemAsync(string path)
        {
            if (_cache == null)
            {
                throw new Exception("Cache is not initialized");
            }

            var cacheEntry = CacheExtensions.Get<TRecord?>(_cache, NormalizePath(path));

            return Task.FromResult(cacheEntry is null
                ? null
                : new StorageItemInfo(cacheEntry.Metadata, cacheEntry.FileSize, cacheEntry.Date,
                    () => cacheEntry.OpenRead()));
        }

        public Task RemoveItemAsync(string path)
        {
            if (_cache == null)
            {
                throw new Exception("Cache is not initialized");
            }

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
