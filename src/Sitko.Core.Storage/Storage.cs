using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Storage
{
    public abstract class Storage : IStorage
    {
        private readonly ILogger<Storage> _logger;
        private readonly StorageOptions _options;

        protected Storage(StorageOptions options, ILogger<Storage> logger)
        {
            _logger = logger;
            _options = options;
        }

        public async Task<StorageItem> SaveFileAsync(byte[] file, string fileName, string path)
        {
            var destinationName = GetStorageFileName(fileName);
            var destinationPath = $"{path}/{destinationName}";

            var tmpPath = Path.GetTempFileName();

            using (var sourceStream = new FileStream(tmpPath,
                FileMode.OpenOrCreate, FileAccess.Write, FileShare.None,
                4096, true))
            {
                await sourceStream.WriteAsync(file, 0, file.Length);
            }

            var storageItem = new StorageItem
            {
                FileName = fileName,
                FileSize = file.LongLength,
                FilePath = destinationPath,
                PublicUri = new Uri($"{_options.PublicUri}/{destinationPath}")
            };

            await DoSaveAsync(destinationPath, tmpPath);
            _logger.LogInformation("File saved to {path}", path);
            return storageItem;
        }

        protected abstract Task<bool> DoSaveAsync(string path, string tmpPath);


        public abstract Task<bool> DeleteFileAsync(string filePath);

        protected string GetStorageFileName(string fileName)
        {
            var extension = fileName.Substring(fileName.LastIndexOf('.'));
            return Guid.NewGuid() + extension;
        }
    }
}
