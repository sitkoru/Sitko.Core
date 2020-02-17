using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace Sitko.Core.Storage.Cache
{
    public class InMemoryStorageCache : IStorageCache<InMemoryStorageCacheOptions>
    {
        private readonly InMemoryStorageCacheOptions _options;

        private readonly ConcurrentDictionary<string, InMemoryStorageCacheRecord> _cache =
            new ConcurrentDictionary<string, InMemoryStorageCacheRecord>();

        public InMemoryStorageCache(InMemoryStorageCacheOptions options)
        {
            _options = options;
        }

        public Task<StorageItem?> GetItemAsync(string path)
        {
            if (_cache.TryGetValue(path, out var record))
            {
                record.DateAccess = DateTimeOffset.UtcNow;
                var item = record.Item;
                item.SetStream(new MemoryStream(record.Data));
                return Task.FromResult(item);
            }

            return Task.FromResult((StorageItem)null);
        }

        public Task<bool> AddItemAsync(string path, StorageItem item)
        {
            if (item.FileSize > _options.MaxFileSizeToStore)
            {
                return Task.FromResult(false);
            }

            var stream = item.CreateReadStream();
            var record = new InMemoryStorageCacheRecord(item, ReadToEnd(stream));
            return Task.FromResult(_cache.TryAdd(path, record));
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

        public Task<bool> IsKeyExistsAsync(string path)
        {
            return Task.FromResult(_cache.ContainsKey(path));
        }

        public Task RemoveItemAsync(string path)
        {
            _cache.TryRemove(path, out _);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _cache.Clear();
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
        public DateTimeOffset DateAdd { get; } = DateTimeOffset.UtcNow;
        public DateTimeOffset DateAccess { get; set; } = DateTimeOffset.UtcNow;
    }

    public class InMemoryStorageCacheRecord : StorageCacheRecord
    {
        public InMemoryStorageCacheRecord(StorageItem item, byte[] data) : base(item)
        {
            Data = data;
        }

        public byte[] Data { get; }
    }
}
