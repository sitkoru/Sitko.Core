using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Storage.Cache
{
    public class FileStorageCache : BaseStorageCache<FileStorageCacheOptions, FileStorageCacheRecord>
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _cleanupTask;

        public FileStorageCache(FileStorageCacheOptions options, ILogger<FileStorageCache> logger) : base(options,
            logger)
        {
            _cleanupTask = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    await Task.Delay(Options.CleanupInterval);
                    Logger.LogInformation("Start deleting obsolete files");
                    Expire();
                    var files = Directory.GetFiles(options.CacheDirectoryPath, "*.*", SearchOption.AllDirectories);
                    var items = this.Select(i => i as FileStorageCacheRecord).ToList();
                    foreach (string file in files)
                    {
                        if (items.Any(i => i.FilePath == file))
                        {
                            continue;
                        }

                        DeleteFile(file);
                    }

                    Logger.LogInformation("Done deleting obsolete files");
                }
            }, _cts.Token);
        }

        protected override void DisposeItem(FileStorageCacheRecord deletedRecord)
        {
            if (!string.IsNullOrEmpty(deletedRecord.FilePath))
            {
                DeleteFile(deletedRecord.FilePath);
            }
        }

        private void DeleteFile(string path)
        {
            Logger.LogInformation("Delete file {File}", path);
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Can't delete file {File}", path);
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

        public override async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            await _cleanupTask;
            if (Directory.Exists(Options.CacheDirectoryPath))
            {
                Directory.Delete(Options.CacheDirectoryPath, true);
            }
        }
    }

    public class FileStorageCacheOptions : StorageCacheOptions
    {
        public string CacheDirectoryPath { get; set; }
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
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
