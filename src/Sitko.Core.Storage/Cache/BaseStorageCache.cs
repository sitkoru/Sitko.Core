using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CacheExtensions = Sitko.Core.Caching.CacheExtensions;
using MemoryCache = Sitko.Core.Caching.MemoryCache;
using MemoryCacheOptions = Sitko.Core.Caching.MemoryCacheOptions;

namespace Sitko.Core.Storage.Cache
{
    public abstract class
        BaseStorageCache<TStorageOptions, TOptions, TRecord> : IStorageCache<TStorageOptions, TOptions>,
            IEnumerable<TRecord>
        where TOptions : StorageCacheOptions
        where TRecord : class, IStorageCacheRecord
        where TStorageOptions : StorageOptions
    {
        protected readonly IOptionsMonitor<TOptions> Options;
        protected readonly ILogger<BaseStorageCache<TStorageOptions, TOptions, TRecord>> Logger;

        private MemoryCache? _cache;

        private readonly ConcurrentDictionary<object, SemaphoreSlim> _locks = new();

        protected BaseStorageCache(IOptionsMonitor<TOptions> options,
            ILogger<BaseStorageCache<TStorageOptions, TOptions, TRecord>> logger)
        {
            Options = options;
            Logger = logger;
            _cache = new MemoryCache(new MemoryCacheOptions {SizeLimit = Options.CurrentValue.MaxCacheSize});
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

        async Task<StorageItemDownloadInfo?> IStorageCache<TStorageOptions>.GetOrAddItemAsync(string path,
            Func<Task<StorageItemDownloadInfo?>> addItem, CancellationToken? cancellationToken)
        {
            if (_cache == null)
            {
                throw new Exception("Cache is not initialized");
            }

            var key = NormalizePath(path);
            if (!CacheExtensions.TryGetValue(_cache, key, out TRecord? cacheEntry)) // Look for cache key.
            {
                var itemLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

                await itemLock.WaitAsync(cancellationToken ?? CancellationToken.None);
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

                        if (Options.CurrentValue.MaxFileSizeToStore > 0 &&
                            result.FileSize > Options.CurrentValue.MaxFileSizeToStore)
                        {
                            Logger.LogWarning(
                                "File {Key} exceed maximum cache file size. File size: {FleSize}. Maximum size: {MaximumSize}",
                                key, result.FileSize,
                                Helpers.HumanSize(Options.CurrentValue.MaxFileSizeToStore));
                            return result;
                        }

                        var stream = result.GetStream();

                        await using (stream)
                        {
                            Logger.LogDebug("Download file {Key}", key);
                            cacheEntry = await GetEntryAsync(result, stream, cancellationToken);

                            var options = new MemoryCacheEntryOptions {SlidingExpiration = Options.CurrentValue.Ttl};
                            options.RegisterPostEvictionCallback((_, value, _, _) =>
                            {
                                if (value is TRecord deletedRecord)
                                {
                                    DisposeItem(deletedRecord);
                                }

                                Logger.LogDebug("Remove file {ObjKey} from cache", key);
                            });
                            if (Options.CurrentValue.MaxCacheSize > 0)
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
                : new StorageItemDownloadInfo( /*cacheEntry.Metadata, */cacheEntry.FileSize, cacheEntry.Date,
                    () => cacheEntry.OpenRead());
        }


        protected abstract void DisposeItem(TRecord deletedRecord);

        internal abstract Task<TRecord> GetEntryAsync(StorageItemDownloadInfo item, Stream stream,
            CancellationToken? cancellationToken = null);

        private string NormalizePath(string path)
        {
            path = new Uri(path, UriKind.Relative).ToString();
            if (!path.StartsWith("/"))
            {
                path = $"/{path}";
            }

            return path;
        }

        Task<StorageItemDownloadInfo?> IStorageCache<TStorageOptions>.GetItemAsync(string path,
            CancellationToken? cancellationToken)
        {
            if (_cache == null)
            {
                throw new Exception("Cache is not initialized");
            }

            var cacheEntry = CacheExtensions.Get<TRecord?>(_cache, NormalizePath(path));

            return Task.FromResult<StorageItemDownloadInfo?>(cacheEntry is null
                ? null
                : new StorageItemDownloadInfo(cacheEntry.FileSize, cacheEntry.Date,
                    () => cacheEntry.OpenRead()));
        }

        public Task RemoveItemAsync(string path, CancellationToken? cancellationToken = null)
        {
            if (_cache == null)
            {
                throw new Exception("Cache is not initialized");
            }

            _cache.Remove(NormalizePath(path));
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken? cancellationToken = null)
        {
            _cache?.Dispose();
            _cache = new MemoryCache(new MemoryCacheOptions {SizeLimit = Options.CurrentValue.MaxCacheSize});
            Logger.LogDebug("Cache cleared");
            return Task.CompletedTask;
        }

        public abstract ValueTask DisposeAsync();
    }
}
