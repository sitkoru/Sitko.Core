using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Storage.Metadata
{
    public abstract class
        BaseStorageMetadataProvider<TOptions, TStorageOptions> : IStorageMetadataProvider<TStorageOptions, TOptions>
        where TOptions : StorageMetadataProviderOptions
        where TStorageOptions : StorageOptions
    {
        protected IOptionsMonitor<TOptions> Options { get; }
        protected IOptionsMonitor<TStorageOptions> StorageOptions { get; }
        protected ILogger<BaseStorageMetadataProvider<TOptions, TStorageOptions>> Logger { get; }

        public BaseStorageMetadataProvider(IOptionsMonitor<TOptions> options,
            IOptionsMonitor<TStorageOptions> storageOptions,
            ILogger<BaseStorageMetadataProvider<TOptions, TStorageOptions>> logger)
        {
            Options = options;
            StorageOptions = storageOptions;
            Logger = logger;
        }

        public abstract ValueTask DisposeAsync();

        Task IStorageMetadataProvider<TStorageOptions>.InitAsync()
        {
            return DoInitAsync();
        }

        protected virtual Task DoInitAsync()
        {
            return Task.CompletedTask;
        }

        Task IStorageMetadataProvider<TStorageOptions>.SaveMetadataAsync(StorageItem storageItem,
            StorageItemMetadata itemMetadata,
            CancellationToken cancellationToken = default)
        {
            return DoSaveMetadataAsync(storageItem, itemMetadata, cancellationToken);
        }

        Task IStorageMetadataProvider<TStorageOptions>.DeleteMetadataAsync(string filePath,
            CancellationToken cancellationToken = default)
        {
            return DoDeleteMetadataAsync(filePath, cancellationToken);
        }

        protected abstract Task DoDeleteMetadataAsync(string filePath, CancellationToken cancellationToken = default);

        Task IStorageMetadataProvider<TStorageOptions>.DeleteAllMetadataAsync(
            CancellationToken cancellationToken = default)
        {
            return DoDeleteAllMetadataAsync(cancellationToken);
        }

        protected abstract Task DoDeleteAllMetadataAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<StorageNode>> IStorageMetadataProvider<TStorageOptions>.GetDirectoryContentAsync(string path,
            CancellationToken cancellationToken = default)
        {
            return DoGetDirectoryContentsAsync(path, cancellationToken);
        }

        protected abstract Task<IEnumerable<StorageNode>> DoGetDirectoryContentsAsync(string path,
            CancellationToken cancellationToken = default);

        async Task IStorageMetadataProvider<TStorageOptions>.RefreshDirectoryContentsAsync(
            IEnumerable<StorageItemInfo> storageItems,
            CancellationToken cancellationToken = default)
        {
            foreach (var storageItem in storageItems)
            {
                await DoSaveMetadataAsync(new StorageItem(storageItem, StorageOptions.CurrentValue.Prefix));
            }
        }

        Task<StorageItemMetadata?> IStorageMetadataProvider<TStorageOptions>.GetMetadataAsync(string path,
            CancellationToken cancellationToken = default)
        {
            return DoGetMetadataAsync(path, cancellationToken);
        }

        protected Task<StorageItemMetadata?> DoGetMetadataAsync(string path,
            CancellationToken cancellationToken = default)
        {
            return DoGetMetadataJsonAsync(path, cancellationToken);
        }

        protected abstract Task<StorageItemMetadata?> DoGetMetadataJsonAsync(string path,
            CancellationToken cancellationToken = default);

        protected abstract Task DoSaveMetadataAsync(StorageItem storageItem, StorageItemMetadata? metadata = null,
            CancellationToken cancellationToken = default);
    }
}
