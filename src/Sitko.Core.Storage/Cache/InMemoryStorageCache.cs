using System;
using System.Buffers;
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
    public class InMemoryStorageCache : IStorageCache<InMemoryStorageCacheOptions>
    {
        private readonly InMemoryStorageCacheOptions _options;
        private readonly ILogger<InMemoryStorageCache> _logger;
        private readonly Dictionary<string, StorageItem> _items = new Dictionary<string, StorageItem>();

        private IMemoryCache _cache;
        private readonly MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;
        private ConcurrentDictionary<object, SemaphoreSlim> _locks = new ConcurrentDictionary<object, SemaphoreSlim>();

        public InMemoryStorageCache(InMemoryStorageCacheOptions options, ILogger<InMemoryStorageCache> logger)
        {
            _options = options;
            _logger = logger;
            InitCache();
        }

        private void InitCache()
        {
            _cache = new MemoryCache(new MemoryCacheOptions {SizeLimit = _options.MaxCacheSize});
        }

        private string NormalizePath(string path)
        {
            path = new Uri(path, UriKind.Relative).ToString();
            if (!path.StartsWith("/"))
            {
                path = $"/{path}";
            }

            return path;
        }

        public Task<StorageItem?> GetItemAsync(string path)
        {
            var record = _cache.Get<InMemoryStorageCacheRecord?>(NormalizePath(path));
            return Task.FromResult(record?.Item);
        }

        public async Task<(StorageItem item, Stream stream)?> GetOrAddItemAsync(string path,
            Func<Task<(StorageItem item, Stream stream)?>> addItem)
        {
            var key = NormalizePath(path);
            if (!_cache.TryGetValue(key, out InMemoryStorageCacheRecord? cacheEntry)) // Look for cache key.
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

                        (StorageItem item, Stream stream) = result.Value;

                        if (_options.MaxFileSizeToStore > 0 && item.FileSize > _options.MaxFileSizeToStore)
                        {
                            _logger.LogWarning(
                                "File {Key} exceed maximum cache file size. File size: {FleSize}. Maximum size: {MaximumSize}",
                                key, item.HumanSize, Helpers.HumanSize(_options.MaxFileSizeToStore));
                            return result.Value;
                        }

                        cacheEntry = new InMemoryStorageCacheRecord(item);


                        if (stream != null)
                        {
#if NETSTANDARD2_1
                            await using (stream)
#else
                                using (stream)
#endif
                            {
                                _logger.LogDebug("Download file {Key}", key);
                                var memoryOwner = _memoryPool.Rent((int)item.FileSize);
                                var bytes = ReadToEnd(stream);
                                for (int i = 0; i < bytes.Length; i++)
                                {
                                    memoryOwner.Memory.Span[i] = bytes[i];
                                }

                                cacheEntry.SetData(memoryOwner);
                            }
                        }

                        var options = new MemoryCacheEntryOptions {SlidingExpiration = _options.Ttl};
                        options.RegisterPostEvictionCallback((objKey, value, reason, state) =>
                        {
                            if (value is InMemoryStorageCacheRecord deletedRecord)
                            {
                                deletedRecord.Data?.Dispose();
                            }

                            _items.Remove(objKey.ToString());
                            _logger.LogDebug("Remove file {ObjKey} from cache", key);
                        });
                        if (_options.MaxCacheSize > 0)
                        {
                            options.Size = item.FileSize;
                        }

                        _logger.LogDebug("Add file {Key} to cache", key);
                        _cache.Set(key, cacheEntry, options);
                        _items.Add(key, item);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.ToString());
                    return null;
                }
                finally
                {
                    itemLock.Release();
                }
            }

            return (cacheEntry.Item, await cacheEntry.GetStreamAsync());
        }

        private static byte[] ReadToEnd(Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }

                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }

        public Task RemoveItemAsync(string path)
        {
            _cache.Remove(NormalizePath(path));
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            InitCache();
            _logger.LogDebug("Cache cleared");
            return Task.CompletedTask;
        }

        public IEnumerator<StorageItem> GetEnumerator()
        {
            return _items.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
