using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Storage.Cache;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage
{
    public abstract class Storage<T> : IStorage<T>, IAsyncDisposable where T : StorageOptions
    {
        protected readonly ILogger<Storage<T>> Logger;
        private readonly IStorageCache? _cache;
        protected readonly IStorageMetadataProvider? _metadataProvider;
        protected readonly T Options;

        protected Storage(T options, ILogger<Storage<T>> logger, IStorageCache? cache,
            IStorageMetadataProvider? metadataProvider)
        {
            Logger = logger;
            _cache = cache;
            _metadataProvider = metadataProvider;
            Options = options;
        }

        public async Task<StorageItem> SaveAsync(Stream file, string fileName, string path, object? metadata = null,
            CancellationToken? cancellationToken = null)
        {
            string destinationPath = GetDestinationPath(fileName, path);

            var itemMetadata = new StorageItemMetadata {FileName = fileName};

            if (metadata != null)
            {
                itemMetadata.SetData(metadata);
            }

            var storageItem = new StorageItem(destinationPath, DateTimeOffset.UtcNow, file.Length, Options.Prefix,
                itemMetadata);

            var result = await SaveStorageItemAsync(file, path, destinationPath, storageItem, itemMetadata,
                cancellationToken);
            if (_metadataProvider != null)
            {
                await _metadataProvider.SaveMetadataAsync(storageItem, itemMetadata, cancellationToken);
            }

            return result;
        }


        private async Task<StorageItem> SaveStorageItemAsync(Stream file, string path, string destinationPath,
            StorageItem storageItem, StorageItemMetadata metadata, CancellationToken? cancellationToken = null)
        {
            file.Seek(0, SeekOrigin.Begin);
            await DoSaveAsync(destinationPath, file, cancellationToken);
            Logger.LogInformation("File saved to {Path}", path);
            if (_cache != null && !string.IsNullOrEmpty(storageItem.FilePath))
            {
                await _cache.RemoveItemAsync(storageItem.FilePath, cancellationToken);
            }

            return storageItem;
        }

        protected virtual string GetDestinationPath(string fileName, string path)
        {
            var destinationName = GetStorageFileName(fileName);
            if (!string.IsNullOrEmpty(Options.Prefix))
            {
                path = Path.Combine(Options.Prefix, path);
            }

            var destinationPath = Helpers.PreparePath(Path.Combine(path, destinationName))!;
            return destinationPath;
        }

        // private StorageItem CreateStorageItem(string path, StorageItemInfo storageItemInfo)
        // {
        //     return CreateStorageItem(path, storageItemInfo.Date,
        //         storageItemInfo.FileSize, storageItemInfo.Metadata);
        // }

        // internal StorageItem CreateStorageItem(string destinationPath,
        //     DateTimeOffset date,
        //     long fileSize, StorageItemMetadata? metadata = null)
        // {
        //     destinationPath = Helpers.GetPathWithoutPrefix(destinationPath);
        //     var fileName = metadata?.FileName ?? Path.GetFileName(destinationPath);
        //     var storageItem = new StorageItem
        //     {
        //         Path = Helpers.PreparePath(Path.GetDirectoryName(destinationPath))!,
        //         FileName = fileName,
        //         LastModified = date,
        //         FileSize = fileSize,
        //         FilePath = destinationPath,
        //         MetadataJson = metadata?.Data,
        //         MimeType = MimeMapping.MimeUtility.GetMimeMapping(fileName)
        //     };
        //     return storageItem;
        // }

        protected abstract Task<bool> DoSaveAsync(string path, Stream file,
            CancellationToken? cancellationToken = null);

        protected abstract Task<bool> DoDeleteAsync(string filePath, CancellationToken? cancellationToken = null);

        protected abstract Task<bool>
            DoIsFileExistsAsync(StorageItem item, CancellationToken? cancellationToken = null);

        protected abstract Task DoDeleteAllAsync(CancellationToken? cancellationToken = null);

        internal abstract Task<StorageItemDownloadInfo?> DoGetFileAsync(string path,
            CancellationToken? cancellationToken = null);

        public async Task<DownloadResult?> DownloadAsync(string path, CancellationToken? cancellationToken = null)
        {
            var info = await GetStorageItemInfoAsync(path, cancellationToken);
            if (info != null)
            {
                var item = new StorageItem(path, info, Options.Prefix);
                return new DownloadResult(item, info.GetStream());
            }

            return null;
        }

        public async Task<bool> DeleteAsync(string filePath, CancellationToken? cancellationToken = null)
        {
            if (_cache != null)
            {
                await _cache.RemoveItemAsync(filePath, cancellationToken);
            }

            var result = await DoDeleteAsync(GetPathWithPrefix(filePath), cancellationToken);
            if (result && _metadataProvider != null)
            {
                await _metadataProvider.DeleteMetadataAsync(filePath, cancellationToken);
            }

            return result;
        }


        protected string GetPathWithPrefix(string filePath)
        {
            if (!string.IsNullOrEmpty(Options.Prefix) && !filePath.StartsWith(Options.Prefix))
            {
                filePath = Helpers.PreparePath(Path.Combine(Options.Prefix, filePath))!;
            }

            return filePath;
        }

        public Task<StorageItem?> GetAsync(string path, CancellationToken? cancellationToken = null)
        {
            return GetStorageItemInternalAsync(path, cancellationToken);
        }

        private async Task<StorageItemDownloadInfo?> GetStorageItemInfoAsync(string path,
            CancellationToken? cancellationToken = null)
        {
            StorageItemDownloadInfo? result;
            if (_cache != null)
            {
                result = await _cache.GetOrAddItemAsync(path,
                    async () => await DoGetFileAsync(GetPathWithPrefix(path)), cancellationToken);
            }
            else
            {
                result = await DoGetFileAsync(GetPathWithPrefix(path), cancellationToken);
                if (result is not null && _metadataProvider is not null)
                {
                    var metadata = await _metadataProvider.GetMetadataAsync(path, cancellationToken);
                    if (metadata is not null)
                    {
                        result.SetMetadata(metadata);
                    }
                }
            }

            return result;
        }

        private async Task<StorageItem?> GetStorageItemInternalAsync(string path,
            CancellationToken? cancellationToken = null)
        {
            var result = await GetStorageItemInfoAsync(path, cancellationToken);

            return result != null
                ? new StorageItem(path, result.Date, result.FileSize, Options.Prefix, result.Metadata)
                : null;
        }


        public async Task<bool> IsExistsAsync(string path, CancellationToken? cancellationToken = null)
        {
            var result = await GetStorageItemInternalAsync(path, cancellationToken);
            return result != null;
        }

        public async Task DeleteAllAsync(CancellationToken? cancellationToken = null)
        {
            if (_cache != null)
            {
                await _cache.ClearAsync(cancellationToken);
            }

            await DoDeleteAllAsync(cancellationToken);
            if (_metadataProvider != null)
            {
                await _metadataProvider.DeleteAllMetadataAsync(cancellationToken);
            }
        }


        public Task<IEnumerable<StorageNode>> GetDirectoryContentsAsync(string path,
            CancellationToken? cancellationToken = null)
        {
            if (_metadataProvider != null)
            {
                return _metadataProvider.GetDirectoryContentAsync(path, cancellationToken);
            }

            throw new Exception("No metadata provider");
        }

        public async Task<IEnumerable<StorageNode>> RefreshDirectoryContentsAsync(string path,
            CancellationToken? cancellationToken = null)
        {
            if (_metadataProvider != null)
            {
                var storageItems = await GetAllItemsAsync(path, cancellationToken);
                await _metadataProvider.RefreshDirectoryContentsAsync(path, storageItems, cancellationToken);
                return await _metadataProvider.GetDirectoryContentAsync(path, cancellationToken);
            }

            throw new Exception("No metadata provider");
        }


        public Uri PublicUri(StorageItem item)
        {
            return PublicUri(item.FilePath);
        }

        public Uri PublicUri(string filePath)
        {
            return new Uri($"{Options.PublicUri}/{filePath}");
        }

        internal abstract Task<IEnumerable<StorageItemInfo>> GetAllItemsAsync(string path,
            CancellationToken? cancellationToken = null);

        Task<IEnumerable<StorageItemInfo>> IStorage.GetAllItemsAsync(string path, CancellationToken? cancellationToken)
        {
            return GetAllItemsAsync(path, cancellationToken);
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
