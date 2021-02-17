using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Storage.Metadata
{
    public abstract class BaseStorageMetadataProvider<TOptions, TStorageOptions> : IStorageMetadataProvider<TOptions>
        where TOptions : StorageMetadataProviderOptions
        where TStorageOptions : StorageOptions
    {
        protected TOptions Options { get; }
        protected TStorageOptions StorageOptions { get; }
        protected ILogger<BaseStorageMetadataProvider<TOptions, TStorageOptions>> Logger { get; }

        public BaseStorageMetadataProvider(TOptions options, TStorageOptions storageOptions,
            ILogger<BaseStorageMetadataProvider<TOptions, TStorageOptions>> logger)
        {
            Options = options;
            StorageOptions = storageOptions;
            Logger = logger;
        }

        public abstract ValueTask DisposeAsync();

        Task IStorageMetadataProvider.InitAsync()
        {
            return DoInitAsync();
        }

        protected virtual Task DoInitAsync()
        {
            return Task.CompletedTask;
        }

        Task IStorageMetadataProvider.SaveMetadataAsync(StorageItem storageItem, StorageItemMetadata itemMetadata,
            CancellationToken? cancellationToken)
        {
            return DoSaveMetadataAsync(storageItem, itemMetadata, cancellationToken);
        }

        Task IStorageMetadataProvider.DeleteMetadataAsync(string filePath, CancellationToken? cancellationToken)
        {
            return DoDeleteMetadataAsync(filePath, cancellationToken);
        }

        protected abstract Task DoDeleteMetadataAsync(string filePath, CancellationToken? cancellationToken);

        Task IStorageMetadataProvider.DeleteAllMetadataAsync(CancellationToken? cancellationToken)
        {
            return DoDeleteAllMetadataAsync(cancellationToken);
        }

        protected abstract Task DoDeleteAllMetadataAsync(CancellationToken? cancellationToken);

        Task<IEnumerable<StorageNode>> IStorageMetadataProvider.GetDirectoryContentAsync(string path,
            CancellationToken? cancellationToken = null)
        {
            return DoGetDirectoryContentsAsync(path, cancellationToken);
        }

        protected abstract Task<IEnumerable<StorageNode>> DoGetDirectoryContentsAsync(string path,
            CancellationToken? cancellationToken = null);

        async Task IStorageMetadataProvider.RefreshDirectoryContentsAsync(IEnumerable<StorageItemInfo> storageItems,
            CancellationToken? cancellationToken)
        {
            foreach (var storageItem in storageItems)
            {
                await DoSaveMetadataAsync(new StorageItem(storageItem, StorageOptions.Prefix));
            }
        }

        Task<StorageItemMetadata?> IStorageMetadataProvider.GetMetadataAsync(string path,
            CancellationToken? cancellationToken)
        {
            return DoGetMetadataAsync(path, cancellationToken);
        }

        protected Task<StorageItemMetadata?> DoGetMetadataAsync(string path, CancellationToken? cancellationToken)
        {
            return DoGetMetadataJsonAsync(path, cancellationToken);
        }

        protected abstract Task<StorageItemMetadata?> DoGetMetadataJsonAsync(string path,
            CancellationToken? cancellationToken = null);

        protected abstract Task DoSaveMetadataAsync(StorageItem storageItem, StorageItemMetadata? metadata = null,
            CancellationToken? cancellationToken = null);
    }
}
