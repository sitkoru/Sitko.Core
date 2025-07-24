using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Storage.Internal;

namespace Sitko.Core.Storage.Metadata;

public abstract class
    BaseStorageMetadataProvider<TOptions, TStorageOptions> : IStorageMetadataProvider<TStorageOptions, TOptions>
    where TOptions : StorageMetadataModuleOptions<TStorageOptions>
    where TStorageOptions : StorageOptions
{
    protected BaseStorageMetadataProvider(IOptionsMonitor<TOptions> options,
        IOptionsMonitor<TStorageOptions> storageOptions,
        ILogger<BaseStorageMetadataProvider<TOptions, TStorageOptions>> logger)
    {
        Options = options;
        StorageOptions = storageOptions;
        Logger = logger;
    }

    protected IOptionsMonitor<TOptions> Options { get; }
    protected IOptionsMonitor<TStorageOptions> StorageOptions { get; }
    protected ILogger<BaseStorageMetadataProvider<TOptions, TStorageOptions>> Logger { get; }

    Task IStorageMetadataProvider<TStorageOptions>.InitAsync(CancellationToken cancellationToken) =>
        DoInitAsync(cancellationToken);

    Task IStorageMetadataProvider<TStorageOptions>.SaveMetadataAsync(StorageItem storageItem,
        StorageItemMetadata itemMetadata, bool isNew,
        CancellationToken cancellationToken) =>
        DoSaveMetadataAsync(storageItem, itemMetadata, isNew, cancellationToken);

    Task IStorageMetadataProvider<TStorageOptions>.DeleteMetadataAsync(string filePath,
        CancellationToken cancellationToken) =>
        DoDeleteMetadataAsync(filePath, cancellationToken);

    Task IStorageMetadataProvider<TStorageOptions>.DeleteAllMetadataAsync(
        CancellationToken cancellationToken) =>
        DoDeleteAllMetadataAsync(cancellationToken);

    Task<IEnumerable<StorageNode>> IStorageMetadataProvider<TStorageOptions>.GetDirectoryContentAsync(string path,
        CancellationToken cancellationToken) =>
        DoGetDirectoryContentsAsync(path, cancellationToken);

    async Task IStorageMetadataProvider<TStorageOptions>.RefreshDirectoryContentsAsync(
        IEnumerable<StorageItemInfo> storageItems,
        CancellationToken cancellationToken)
    {
        foreach (var storageItem in storageItems)
        {
            await DoSaveMetadataAsync(storageItem.GetStorageItem(), cancellationToken: cancellationToken);
        }
    }

    Task<StorageItemMetadata?> IStorageMetadataProvider<TStorageOptions>.GetMetadataAsync(string path,
        CancellationToken cancellationToken) =>
        DoGetMetadataAsync(path, cancellationToken);

    protected virtual Task DoInitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    protected abstract Task DoDeleteMetadataAsync(string filePath, CancellationToken cancellationToken = default);

    protected abstract Task DoDeleteAllMetadataAsync(CancellationToken cancellationToken = default);

    protected abstract Task<IEnumerable<StorageNode>> DoGetDirectoryContentsAsync(string path,
        CancellationToken cancellationToken = default);

    protected Task<StorageItemMetadata?> DoGetMetadataAsync(string path,
        CancellationToken cancellationToken = default) =>
        DoGetMetadataJsonAsync(path, cancellationToken);

    protected abstract Task<StorageItemMetadata?> DoGetMetadataJsonAsync(string path,
        CancellationToken cancellationToken = default);

    protected abstract Task DoSaveMetadataAsync(StorageItem storageItem, StorageItemMetadata? metadata = null,
        bool isNew = true,
        CancellationToken cancellationToken = default);
}
