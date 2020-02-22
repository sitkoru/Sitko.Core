using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Storage.Cache;

namespace Sitko.Core.Storage
{
    public abstract class Storage<T> : IStorage<T>, IAsyncDisposable where T : StorageOptions
    {
        protected readonly ILogger<Storage<T>> Logger;
        private readonly IStorageCache? _cache;
        private readonly T _options;

        protected Storage(T options, ILogger<Storage<T>> logger, IStorageCache? cache)
        {
            Logger = logger;
            _cache = cache;
            _options = options;
        }

        public async Task<StorageItem> SaveFileAsync(Stream file, string fileName, string path)
        {
            string destinationPath = GetDestinationPath(fileName, path);

            var storageItem = CreateStorageItem(file, fileName, destinationPath);

            return await SaveStorageItemAsync(file, path, destinationPath, storageItem);
        }


        private async Task<StorageItem> SaveStorageItemAsync(Stream file, string path, string destinationPath,
            StorageItem storageItem)
        {
            file.Seek(0, SeekOrigin.Begin);
            await DoSaveAsync(destinationPath, file);
            Logger.LogInformation("File saved to {Path}", path);
            if (_cache != null)
            {
                await _cache.RemoveItemAsync(storageItem.FilePath);
            }

            return storageItem;
        }

        private string GetDestinationPath(string fileName, string path)
        {
            var destinationName = GetStorageFileName(fileName);
            var destinationPath = $"{path}/{destinationName}".Replace("\\", "/").Replace("//", "/");
            return destinationPath;
        }

        private StorageItem CreateStorageItem(Stream file, string fileName, string destinationPath)
        {
            var storageItem = new StorageItem
            {
                FileName = fileName,
                FileSize = file.Length,
                FilePath = destinationPath,
                Path = Path.GetDirectoryName(destinationPath)?.Replace("\\", "/"),
            };
            return storageItem;
        }

        protected abstract Task<bool> DoSaveAsync(string path, Stream file);
        protected abstract Task<bool> DoDeleteAsync(string filePath);

        protected abstract Task<bool> DoIsFileExistsAsync(StorageItem item);
        protected abstract Task DoDeleteAllAsync();
        protected abstract Task<StorageRecord?> DoGetFileAsync(string path);

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            if (_cache != null)
            {
                await _cache.RemoveItemAsync(filePath);
            }

            return await DoDeleteAsync(filePath);
        }

        public Task<StorageRecord?> GetFileAsync(string path)
        {
            return GetFileInternalAsync(path);
        }

        protected virtual Task<StorageRecord?> GetFileInternalAsync(string path)
        {
            if (_cache != null)
            {
                return _cache.GetOrAddItemAsync(path, () => DoGetFileAsync(path));
            }

            return DoGetFileAsync(path);
        }


        public async Task<bool> IsFileExistsAsync(string path)
        {
            var result = await GetFileInternalAsync(path);
            return result != null;
        }

        public async Task DeleteAllAsync()
        {
            if (_cache != null)
            {
                await _cache.ClearAsync();
            }

            await DoDeleteAllAsync();
        }

        public abstract Task<StorageItemCollection> GetDirectoryContentsAsync(string path);

        public Uri PublicUri(StorageItem item)
        {
            return new Uri($"{_options.PublicUri}/{item.FilePath}");
        }

        private string GetStorageFileName(string fileName)
        {
            var extension = fileName.Substring(fileName.LastIndexOf('.'));
            return Guid.NewGuid() + extension;
        }

        public virtual ValueTask DisposeAsync()
        {
            return new ValueTask();
        }
    }
}
