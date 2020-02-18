using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Storage.Cache
{
    public class InMemoryStorageCache : IStorageCache<InMemoryStorageCacheOptions>
    {
        private readonly InMemoryStorageCacheOptions _options;
        private readonly ILogger<InMemoryStorageCache> _logger;

        private IAppCache _cache;
        private MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;

        public InMemoryStorageCache(InMemoryStorageCacheOptions options, ILogger<InMemoryStorageCache> logger)
        {
            _options = options;
            _logger = logger;
            InitCache();
        }

        private void InitCache()
        {
            _cache = new CachingService();
        }

        public async Task<StorageItem?> GetItemAsync(string path)
        {
            var record = await _cache.GetAsync<InMemoryStorageCacheRecord>(path);
            return record?.GetItem();
        }

        public async Task<StorageItem?> GetOrAddItemAsync(string path, Func<Task<StorageItem>> addItem)
        {
            var options = new MemoryCacheEntryOptions {SlidingExpiration = _options.Ttl};
            options.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                if (value is InMemoryStorageCacheRecord deletedRecord)
                {
                    deletedRecord.Data.Dispose();
                }
                _logger.LogDebug("Remove file {Key} from cache", key);
            });
            var record = await _cache.GetOrAddAsync(path, async () =>
            {
                var item = await addItem();
                if (_options.MaxFileSizeToStore > 0 && item.FileSize > _options.MaxFileSizeToStore)
                {
                    return null;
                }

                var memoryOwner = _memoryPool.Rent((int)item.FileSize);
                var stream = item.CreateReadStream();
                var bytes = ReadToEnd(stream);
                for (int i = 0; i < bytes.Length; i++)
                {
                    memoryOwner.Memory.Span[i] = bytes[i];
                }
                _logger.LogDebug("Add file {Key} to cache", path);
                return new InMemoryStorageCacheRecord(item, memoryOwner);
            }, options);

            return record?.GetItem();
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
            _cache.Remove(path);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            InitCache();
            _logger.LogDebug("Cache cleared");
            return Task.CompletedTask;
        }
    }

    public class StorageCacheRecord
    {
        protected StorageCacheRecord(StorageItem item)
        {
            Item = item;
        }

        public StorageItem Item { get; }
    }

    public class InMemoryStorageCacheRecord : StorageCacheRecord
    {
        public InMemoryStorageCacheRecord(StorageItem item, IMemoryOwner<byte> data) : base(item)
        {
            Data = data;
        }

        public IMemoryOwner<byte> Data { get; }

        public StorageItem GetItem()
        {
            var item = Item;
            var stream = new MemoryStream(Data.Memory.ToArray());
            item.SetStream(stream);
            return item;
        }
    }
}
