using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Storage.Cache
{
    public class FileStorageCache : BaseStorageCache<FileStorageCacheOptions, FileStorageCacheRecord>
    {
        public FileStorageCache(FileStorageCacheOptions options, ILogger<FileStorageCache> logger) : base(options,
            logger)
        {
        }

        protected override void DisposeItem(FileStorageCacheRecord deletedRecord)
        {
            if (!string.IsNullOrEmpty(deletedRecord.FilePath))
            {
                File.Delete(deletedRecord.FilePath);
            }
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                foreach (var t in hashBytes)
                {
                    sb.Append(t.ToString("X2"));
                }

                return sb.ToString();
            }
        }

        protected override async Task<FileStorageCacheRecord> GetEntryAsync(StorageItem item, Stream stream)
        {
            var tempFileName = CreateMD5(item.FilePath);
            var split = tempFileName.Select((c, index) => new {c, index})
                .GroupBy(x => x.index / 2)
                .Select(group => group.Select(elem => elem.c))
                .Select(chars => new string(chars.ToArray())).ToArray();
            var path = Path.Combine(split);
            var directoryPath = Path.Combine(Options.CacheDirectoryPath, path);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var filePath = Path.Combine(directoryPath, item.FileName);

            var fileStream = File.OpenWrite(filePath);
            if (!fileStream.CanWrite)
            {
                throw new Exception($"Can't write to file {filePath}");
            }

            await stream.CopyToAsync(fileStream);
            fileStream.Close();
            return new FileStorageCacheRecord(item, filePath);
        }

        protected override Task<StorageRecord> GetStorageRecord(FileStorageCacheRecord record)
        {
            return Task.FromResult(new StorageRecord(record.Item, record.FilePath));
        }

        public override ValueTask DisposeAsync()
        {
            if (Directory.Exists(Options.CacheDirectoryPath))
            {
                Directory.Delete(Options.CacheDirectoryPath, true);
            }

            return new ValueTask();
        }
    }

    public class FileStorageCacheOptions : StorageCacheOptions
    {
        public string CacheDirectoryPath { get; set; }
    }

    public class FileStorageCacheRecord : StorageCacheRecord
    {
        public FileStorageCacheRecord(StorageItem item, string filePath) : base(item)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }
    }
}
