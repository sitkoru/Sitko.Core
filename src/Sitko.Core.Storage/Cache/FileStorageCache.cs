using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Cache
{
    public class
        FileStorageCache<TStorageOptions> : BaseStorageCache<TStorageOptions, FileStorageCacheOptions,
            FileStorageCacheRecord> where TStorageOptions : StorageOptions
    {
        private readonly CancellationTokenSource cts = new();
        private readonly Task cleanupTask;

        public FileStorageCache(IOptionsMonitor<FileStorageCacheOptions> options,
            ILogger<FileStorageCache<TStorageOptions>> logger) : base(options,
            logger) =>
            cleanupTask = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(Options.CurrentValue.CleanupIntervalInMinutes));
                    Logger.LogInformation("Start deleting obsolete files");
                    Expire();
                    Logger.LogInformation("Done deleting obsolete files");
                }
            }, cts.Token);

        protected override void DisposeItem(FileStorageCacheRecord deletedRecord)
        {
            if (!string.IsNullOrEmpty(deletedRecord.PhysicalPath))
            {
                DeleteFile(deletedRecord.PhysicalPath);
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

        private static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new();
            foreach (var t in hashBytes)
            {
                sb.Append(t.ToString("X2"));
            }

            return sb.ToString();
        }

        internal override async Task<FileStorageCacheRecord> GetEntryAsync(StorageItemDownloadInfo item,
            Stream stream, CancellationToken cancellationToken = default)
        {
            var tempFileName = CreateMD5(Guid.NewGuid().ToString());
            var split = tempFileName.Select((c, index) => new {c, index})
                .GroupBy(x => x.index / 2)
                .Select(group => group.Select(elem => elem.c))
                .Select(chars => new string(chars.ToArray())).ToArray();
            var dirPath = Path.Combine(split);
            var directoryPath = Path.Combine(Options.CurrentValue.CacheDirectoryPath, dirPath);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var filePath = Path.Combine(directoryPath, Guid.NewGuid().ToString());

            var fileStream = File.OpenWrite(filePath);
            if (!fileStream.CanWrite)
            {
                throw new Exception($"Can't write to file {filePath}");
            }

            await stream.CopyToAsync(fileStream, cancellationToken);
            fileStream.Close();
            return new FileStorageCacheRecord(item.Metadata, item.FileSize, item.Date, filePath);
        }

        public override async ValueTask DisposeAsync()
        {
            cts.Cancel();
            await cleanupTask;
            if (Directory.Exists(Options.CurrentValue.CacheDirectoryPath))
            {
                Directory.Delete(Options.CurrentValue.CacheDirectoryPath, true);
            }
        }
    }

    public class FileStorageCacheOptions : StorageCacheOptions
    {
        public string CacheDirectoryPath { get; set; } = "";
        public int CleanupIntervalInMinutes { get; set; } = 60;
    }

    public class FileStorageCacheRecord : IStorageCacheRecord
    {
        internal FileStorageCacheRecord(StorageItemMetadata? metadata, long fileSize, DateTimeOffset date,
            string filePath)
        {
            Metadata = metadata;
            FileSize = fileSize;
            PhysicalPath = filePath;
            Date = date;
        }

        public string PhysicalPath { get; }

        public StorageItemMetadata? Metadata { get; }
        public long FileSize { get; }
        public DateTimeOffset Date { get; }

        public Stream OpenRead()
        {
            var fileInfo = new FileInfo(PhysicalPath);
            if (fileInfo.Exists)
            {
                return fileInfo.OpenRead();
            }

            throw new Exception($"File {PhysicalPath} doesn't exists");
        }
    }
}
