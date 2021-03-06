﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Storage.Metadata
{
    public abstract class
        BaseStorageMetadataProvider<TOptions, TStorageOptions> : IStorageMetadataProvider<TStorageOptions, TOptions>
        where TOptions : StorageMetadataModuleOptions<TStorageOptions>
        where TStorageOptions : StorageOptions
    {
        protected IOptionsMonitor<TOptions> Options { get; }
        protected IOptionsMonitor<TStorageOptions> StorageOptions { get; }
        protected ILogger<BaseStorageMetadataProvider<TOptions, TStorageOptions>> Logger { get; }

        protected BaseStorageMetadataProvider(IOptionsMonitor<TOptions> options,
            IOptionsMonitor<TStorageOptions> storageOptions,
            ILogger<BaseStorageMetadataProvider<TOptions, TStorageOptions>> logger)
        {
            Options = options;
            StorageOptions = storageOptions;
            Logger = logger;
        }

        public abstract ValueTask DisposeAsync();

        Task IStorageMetadataProvider<TStorageOptions>.InitAsync() => DoInitAsync();

        protected virtual Task DoInitAsync() => Task.CompletedTask;

        Task IStorageMetadataProvider<TStorageOptions>.SaveMetadataAsync(StorageItem storageItem,
            StorageItemMetadata itemMetadata,
            CancellationToken cancellationToken) =>
            DoSaveMetadataAsync(storageItem, itemMetadata, cancellationToken);

        Task IStorageMetadataProvider<TStorageOptions>.DeleteMetadataAsync(string filePath,
            CancellationToken cancellationToken) =>
            DoDeleteMetadataAsync(filePath, cancellationToken);

        protected abstract Task DoDeleteMetadataAsync(string filePath, CancellationToken cancellationToken = default);

        Task IStorageMetadataProvider<TStorageOptions>.DeleteAllMetadataAsync(
            CancellationToken cancellationToken) =>
            DoDeleteAllMetadataAsync(cancellationToken);

        protected abstract Task DoDeleteAllMetadataAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<StorageNode>> IStorageMetadataProvider<TStorageOptions>.GetDirectoryContentAsync(string path,
            CancellationToken cancellationToken) =>
            DoGetDirectoryContentsAsync(path, cancellationToken);

        protected abstract Task<IEnumerable<StorageNode>> DoGetDirectoryContentsAsync(string path,
            CancellationToken cancellationToken = default);

        async Task IStorageMetadataProvider<TStorageOptions>.RefreshDirectoryContentsAsync(
            IEnumerable<StorageItemInfo> storageItems,
            CancellationToken cancellationToken)
        {
            foreach (var storageItem in storageItems)
            {
                await DoSaveMetadataAsync(new StorageItem(storageItem, StorageOptions.CurrentValue.Prefix), cancellationToken: cancellationToken);
            }
        }

        Task<StorageItemMetadata?> IStorageMetadataProvider<TStorageOptions>.GetMetadataAsync(string path,
            CancellationToken cancellationToken) =>
            DoGetMetadataAsync(path, cancellationToken);

        protected Task<StorageItemMetadata?> DoGetMetadataAsync(string path,
            CancellationToken cancellationToken = default) =>
            DoGetMetadataJsonAsync(path, cancellationToken);

        protected abstract Task<StorageItemMetadata?> DoGetMetadataJsonAsync(string path,
            CancellationToken cancellationToken = default);

        protected abstract Task DoSaveMetadataAsync(StorageItem storageItem, StorageItemMetadata? metadata = null,
            CancellationToken cancellationToken = default);
    }
}
