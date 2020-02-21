using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        public async Task<Stream?> GetItemStreamAsync(string path)
        {
            var record = _cache.Get<InMemoryStorageCacheRecord?>(NormalizePath(path));
            if (record == null)
            {
                return null;
            }

            return await record.GetStreamAsync();
        }

        public async Task<StorageItem?> GetOrAddItemAsync(string path, Func<Task<StorageItem>> addItem)
        {
            var record = await GetOrAddRecordAsync(path, addItem);

            return record?.Item;
        }

        private async Task<InMemoryStorageCacheRecord?> GetOrAddRecordAsync(string path,
            Func<Task<StorageItem>> addItem)
        {
            try
            {
                var record = await _cache.GetOrCreateAsync(NormalizePath(path), async entry =>
                {
                    entry.SlidingExpiration = _options.Ttl;
                    entry.RegisterPostEvictionCallback((key, value, reason, state) =>
                    {
                        if (value is InMemoryStorageCacheRecord deletedRecord)
                        {
                            deletedRecord.Data?.Dispose();
                        }

                        _items.Remove(key.ToString());
                        _logger.LogDebug("Remove file {Key} from cache", key);
                    });
                    var item = await addItem();

                    if (item == null)
                    {
                        throw new Exception($"File {entry.Key} not found");
                    }

                    if (_options.MaxFileSizeToStore > 0 && item.FileSize > _options.MaxFileSizeToStore)
                    {
                        return null;
                    }

                    if (_options.MaxCacheSize > 0)
                    {
                        entry.Size = item.FileSize;
                    }

                    _logger.LogDebug("Add file {Key} to cache", entry.Key);
                    _items.Add(entry.Key.ToString(), item);
                    return new InMemoryStorageCacheRecord(item);
                });
                return record;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Stream?> GetOrAddItemStreamAsync(string path,
            Func<Task<(StorageItem item, Stream stream)?>> addItem)
        {
            Stream stream = null;
            var record = await GetOrAddRecordAsync(path, async () =>
            {
                var result = await addItem();
                if (result == null)
                {
                    return null;
                }

                stream = result.Value.stream;
                return result.Value.item;
            });
            if (record == null)
            {
                return null;
            }

            if (record.Data == null)
            {
                _logger.LogDebug("Download file {File}", path);
                stream ??= (await addItem())?.stream;
                if (stream == null)
                {
                    return null;
                }

                var memoryOwner = _memoryPool.Rent((int)record.Item.FileSize);
                var bytes = ReadToEnd(stream);
                for (int i = 0; i < bytes.Length; i++)
                {
                    memoryOwner.Memory.Span[i] = bytes[i];
                }

                record.SetData(memoryOwner);
            }

            return await record.GetStreamAsync();
        }

        public static byte[] ReadToEnd(Stream stream)
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
