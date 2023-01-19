using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Sitko.Core.App.Helpers;
using Sitko.Core.Storage.Internal;

namespace Sitko.Core.Storage.Cache;

public abstract class
    BaseStorageCache<TStorageOptions, TOptions, TRecord> : IStorageCache<TStorageOptions, TOptions>
    where TOptions : StorageCacheOptions
    where TRecord : class, IStorageCacheRecord
    where TStorageOptions : StorageOptions
{
    private MemoryCache? cache;

    protected BaseStorageCache(IOptionsMonitor<TOptions> options,
        ILogger<BaseStorageCache<TStorageOptions, TOptions, TRecord>> logger)
    {
        Options = options;
        Logger = logger;
        cache = CreateCache();
    }

    protected IOptionsMonitor<TOptions> Options { get; }
    protected ILogger<BaseStorageCache<TStorageOptions, TOptions, TRecord>> Logger { get; }

    Task<StorageItemDownloadInfo?> IStorageCache<TStorageOptions>.GetOrAddItemAsync(string path,
        Func<Task<StorageItemDownloadInfo?>> addItem, CancellationToken cancellationToken)
    {
        if (cache == null)
        {
            throw new InvalidOperationException("Cache is not initialized");
        }

        var key = NormalizePath(path);
        return cache.GetOrCreateAsync(key, async cacheEntry =>
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
                    FilesHelper.HumanSize(Options.CurrentValue.MaxFileSizeToStore));
                return result;
            }

            var stream = await result.GetStreamAsync();

            await using (stream)
            {
                Logger.LogDebug("Download file {Key}", key);
                var record = await GetEntryAsync(result, stream, cancellationToken);
                ConfigureCacheEntry(cacheEntry);
                if (Options.CurrentValue.MaxCacheSize > 0)
                {
                    cacheEntry.Size = result.FileSize;
                }

                Logger.LogDebug("Add file {Key} to cache", key);
                return new StorageItemDownloadInfo(path, record.FileSize, record.Date,
                    () => Task.FromResult(record.OpenRead()));
            }
        });
    }

    Task<StorageItemDownloadInfo?> IStorageCache<TStorageOptions>.GetItemAsync(string path,
        CancellationToken cancellationToken)
    {
        if (cache == null)
        {
            throw new InvalidOperationException("Cache is not initialized");
        }

        var cacheEntry = cache.Get<TRecord?>(NormalizePath(path));

        return Task.FromResult(cacheEntry is null
            ? null
            : new StorageItemDownloadInfo(path, cacheEntry.FileSize, cacheEntry.Date,
                () => Task.FromResult(cacheEntry.OpenRead())));
    }

    public Task RemoveItemAsync(string path, CancellationToken cancellationToken = default)
    {
        if (cache == null)
        {
            throw new InvalidOperationException("Cache is not initialized");
        }

        cache.Remove(NormalizePath(path));
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cache?.Dispose();
        cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = Options.CurrentValue.MaxCacheSize });
        Logger.LogDebug("Cache cleared");
        return Task.CompletedTask;
    }

    public abstract ValueTask DisposeAsync();

    private MemoryCache CreateCache() => new(new MemoryCacheOptions { SizeLimit = Options.CurrentValue.MaxCacheSize });

    protected void Expire() => cache = CreateCache();

    protected virtual void ConfigureCacheEntry(ICacheEntry entry)
    {
        var expirationTime = DateTime.Now.Add(TimeSpan.FromMinutes(Options.CurrentValue.TtlInMinutes));
        var expirationToken = new CancellationChangeToken(
            new CancellationTokenSource(TimeSpan.FromMinutes(Options.CurrentValue.TtlInMinutes).Add(
                TimeSpan.FromSeconds(1))).Token);
        entry
            // Pin to cache.
            .SetPriority(CacheItemPriority.NeverRemove)
            // Set the actual expiration time
            .SetAbsoluteExpiration(expirationTime)
            // Force eviction to run
            .AddExpirationToken(expirationToken)
            // Add eviction callback
            .RegisterPostEvictionCallback(CacheItemRemoved, this);
    }

    private void CacheItemRemoved(object key, object? value, EvictionReason reason, object? state)
    {
        if (value is TRecord deletedRecord)
        {
            DisposeItem(deletedRecord);
        }

        Logger.LogDebug("Remove file {ObjKey} from cache", key);
    }


    protected abstract void DisposeItem(TRecord deletedRecord);

    internal abstract Task<TRecord> GetEntryAsync(StorageItemDownloadInfo item, Stream stream,
        CancellationToken cancellationToken = default);

    private static string NormalizePath(string path)
    {
        path = new Uri(path, UriKind.Relative).ToString();
        if (!path.StartsWith("/", StringComparison.InvariantCulture))
        {
            path = $"/{path}";
        }

        return path;
    }
}

