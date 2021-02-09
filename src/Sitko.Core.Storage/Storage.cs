using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Sitko.Core.Storage.Cache;

namespace Sitko.Core.Storage
{
    public abstract class Storage<T> : IStorage<T>, IAsyncDisposable where T : StorageOptions
    {
        protected readonly ILogger<Storage<T>> Logger;
        private readonly IStorageCache? _cache;
        private readonly T _options;
        private StorageNode? _tree;
        private DateTimeOffset? _treeLastBuild;
        private AsyncLock _treeLock = new();

        protected Storage(T options, ILogger<Storage<T>> logger, IStorageCache? cache)
        {
            Logger = logger;
            _cache = cache;
            _options = options;
        }

        public async Task<StorageItem> SaveAsync(Stream file, string fileName, string path, object? metadata = null)
        {
            string destinationPath = GetDestinationPath(fileName, path);

            var itemMetadata = new StorageItemMetadata {FileName = fileName};

            if (metadata != null)
            {
                itemMetadata.SetData(metadata);
            }

            var storageItem = CreateStorageItem(destinationPath, DateTimeOffset.UtcNow, file.Length,
                itemMetadata);

            var result = await SaveStorageItemAsync(file, path, destinationPath, storageItem, itemMetadata);
            if (_tree != null)
            {
                _tree.AddItem(storageItem);
            }

            return result;
        }


        private async Task<StorageItem> SaveStorageItemAsync(Stream file, string path, string destinationPath,
            StorageItem storageItem, StorageItemMetadata metadata)
        {
            file.Seek(0, SeekOrigin.Begin);
            await DoSaveAsync(destinationPath, file, JsonSerializer.Serialize(metadata));
            Logger.LogInformation("File saved to {Path}", path);
            if (_cache != null && !string.IsNullOrEmpty(storageItem.FilePath))
            {
                await _cache.RemoveItemAsync(storageItem.FilePath);
            }

            return storageItem;
        }

        private string GetDestinationPath(string fileName, string path)
        {
            var destinationName = GetStorageFileName(fileName);
            var destinationPath = PreparePath($"{path}/{destinationName}")!;
            return destinationPath;
        }

        private StorageItem CreateStorageItem(string path, StorageItemInfo storageItemInfo)
        {
            return CreateStorageItem(path, storageItemInfo.Date, storageItemInfo.FileSize, storageItemInfo.Metadata);
        }

        protected StorageItem CreateStorageItem(string destinationPath,
            DateTimeOffset date,
            long fileSize,
            string? metadata)
        {
            return CreateStorageItem(destinationPath, date, fileSize,
                string.IsNullOrEmpty(metadata)
                    ? new StorageItemMetadata {FileName = Path.GetFileName(destinationPath)}
                    : JsonSerializer.Deserialize<StorageItemMetadata>(metadata)!);
        }


        private StorageItem CreateStorageItem(string destinationPath,
            DateTimeOffset date,
            long fileSize,
            StorageItemMetadata metadata)
        {
            var fileName = metadata.FileName ?? Path.GetFileName(destinationPath);
            var storageItem = new StorageItem
            {
                Path = PreparePath(Path.GetDirectoryName(destinationPath))!,
                FileName = fileName,
                LastModified = date,
                FileSize = fileSize,
                FilePath = destinationPath,
                MetadataJson = metadata.Data,
                MimeType = MimeMapping.MimeUtility.GetMimeMapping(fileName)
            };
            return storageItem;
        }

        protected abstract Task<bool> DoSaveAsync(string path, Stream file, string metadata);
        protected abstract Task<bool> DoDeleteAsync(string filePath);

        protected abstract Task<bool> DoIsFileExistsAsync(StorageItem item);
        protected abstract Task DoDeleteAllAsync();
        internal abstract Task<StorageItemInfo?> DoGetFileAsync(string path);

        public async Task<DownloadResult?> DownloadAsync(string path)
        {
            var info = await GetStorageItemInfoAsync(path);
            if (info != null)
            {
                var item = CreateStorageItem(path, info);
                return new DownloadResult(item, info.GetStream());
            }

            return null;
        }

        public async Task<bool> DeleteAsync(string filePath)
        {
            if (_cache != null)
            {
                await _cache.RemoveItemAsync(filePath);
            }

            var result = await DoDeleteAsync(filePath);
            if (_tree != null)
            {
                _tree.RemoveItem(filePath);
            }

            return result;
        }

        public Task<StorageItem?> GetAsync(string path)
        {
            return GetStorageItemInternalAsync(path);
        }

        private async Task<StorageItemInfo?> GetStorageItemInfoAsync(string path)
        {
            StorageItemInfo? result;
            if (_cache != null)
            {
                result = await _cache.GetOrAddItemAsync(path, async () => await DoGetFileAsync(path));
            }
            else
            {
                result = await DoGetFileAsync(path);
            }

            return result;
        }

        private async Task<StorageItem?> GetStorageItemInternalAsync(string path)
        {
            var result = await GetStorageItemInfoAsync(path);

            return result != null ? CreateStorageItem(path, result.Date, result.FileSize, result.Metadata) : null;
        }


        public async Task<bool> IsExistsAsync(string path)
        {
            var result = await GetStorageItemInternalAsync(path);
            return result != null;
        }

        public async Task DeleteAllAsync()
        {
            if (_cache != null)
            {
                await _cache.ClearAsync();
            }

            await DoDeleteAllAsync();
            _tree = null;
            _treeLastBuild = null;
        }


        public async Task<IEnumerable<StorageNode>> GetDirectoryContentsAsync(string path)
        {
            if (_tree == null || _treeLastBuild < DateTimeOffset.UtcNow.Subtract(_options.StorageTreeCacheTimeout))
            {
                await BuildStorageTreeAsync();
            }

            if (_tree == null) { return new List<StorageNode>(); }

            var parts = PreparePath(path.Trim('/'))!.Split("/");
            var current = _tree;
            foreach (var part in parts)
            {
                current = current?.Children.Where(n => n.Type == StorageNodeType.Directory)
                    .FirstOrDefault(f => f.Name == part);
            }

            return current?.Children ?? new StorageNode[0];
        }

        public async Task<IEnumerable<StorageNode>> RefreshDirectoryContentsAsync(string path)
        {
            await BuildStorageTreeAsync();
            return await GetDirectoryContentsAsync(path);
        }

        private TaskCompletionSource<bool>? _treeBuildTaskSource;

        private async Task BuildStorageTreeAsync()
        {
            if (_treeBuildTaskSource != null)
            {
                await _treeBuildTaskSource.Task;
                return;
            }

            using (await _treeLock.LockAsync())
            {
                Logger.LogInformation("Start building storage tree");
                _treeBuildTaskSource = new TaskCompletionSource<bool>();
                _tree = await DoBuildStorageTreeAsync();
                _treeLastBuild = DateTimeOffset.UtcNow;
                _treeBuildTaskSource.SetResult(true);
                _treeBuildTaskSource = null;
                Logger.LogInformation("Done building storage tree");
            }
        }

        protected abstract Task<StorageNode?> DoBuildStorageTreeAsync();

        public Uri PublicUri(StorageItem item)
        {
            return PublicUri(item.FilePath);
        }

        public Uri PublicUri(string filePath)
        {
            return new Uri($"{_options.PublicUri}/{filePath}");
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

        protected string? PreparePath(string? path)
        {
            return path?.Replace("\\", "/").Replace("//", "/");
        }

        protected const string MetaDataExtension = ".metadata";

        protected string GetMetaDataPath(string filePath)
        {
            return filePath + MetaDataExtension;
        }
    }
}
