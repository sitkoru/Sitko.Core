using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Storage.Cache;
using Sitko.Core.Storage.Internal;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage;

public abstract class Storage<TStorageOptions> : IStorage<TStorageOptions>, IAsyncDisposable
    where TStorageOptions : StorageOptions
{
    private readonly IStorageCache<TStorageOptions>? cache;
    private readonly IOptionsMonitor<TStorageOptions> optionsMonitor;

    protected Storage(IOptionsMonitor<TStorageOptions> options, ILogger<Storage<TStorageOptions>> logger,
        IStorageCache<TStorageOptions>? cache,
        IStorageMetadataProvider<TStorageOptions>? metadataProvider)
    {
        Logger = logger;
        this.cache = cache;
        MetadataProvider = metadataProvider;
        if (metadataProvider is IEmbedStorageMetadataProvider embedStorageMetadataProvider)
        {
            embedStorageMetadataProvider.SetStorage(this);
        }

        optionsMonitor = options;
    }

    protected ILogger<Storage<TStorageOptions>> Logger { get; }
    protected IStorageMetadataProvider<TStorageOptions>? MetadataProvider { get; }

    protected TStorageOptions Options => optionsMonitor.CurrentValue;

    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return new ValueTask();
    }

    public async Task<StorageItem> SaveAsync(Stream file, string fileName, string path, object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var itemMetadata = new StorageItemMetadata { FileName = fileName };

        if (metadata != null)
        {
            itemMetadata.SetData(metadata);
        }

        var request = new UploadRequest(file, path, fileName, itemMetadata);
        // var storageItem = new StorageItem(destinationPath, DateTimeOffset.UtcNow, file.Length, Options.Prefix,
        //     itemMetadata);

        var storageItem = await SaveStorageItemAsync(request, cancellationToken);
        if (MetadataProvider != null)
        {
            await MetadataProvider.SaveMetadataAsync(storageItem, itemMetadata, true, cancellationToken);
        }

        return storageItem;
    }

    public async Task<StorageItem> UpdateMetaDataAsync(StorageItem item, string fileName,
        object? metadata = null, CancellationToken cancellationToken = default)
    {
        if (MetadataProvider is null)
        {
            throw new InvalidOperationException("No metadata provider");
        }

        Logger.LogDebug("Update metadata for item {Path}", item.FilePath);
        var itemMetadata = new StorageItemMetadata { FileName = fileName };

        if (metadata != null)
        {
            itemMetadata.SetData(metadata);
        }

        await MetadataProvider.SaveMetadataAsync(item, itemMetadata, false, cancellationToken);
        item = (await GetStorageItemInternalAsync(item.FilePath, cancellationToken))!;
        return item;
    }

    public async Task<DownloadResult?> DownloadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var info = await GetStorageItemInfoAsync(filePath, cancellationToken);
        if (info != null)
        {
            var item = info.StorageItem;
            return new DownloadResult(item, await info.GetStreamAsync());
        }

        return null;
    }

    public async Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (cache != null)
        {
            await cache.RemoveItemAsync(filePath, cancellationToken);
        }

        var result = await DoDeleteAsync(GetPathWithPrefix(filePath), cancellationToken);
        if (result && MetadataProvider != null)
        {
            await MetadataProvider.DeleteMetadataAsync(filePath, cancellationToken);
        }

        return result;
    }

    public Task<StorageItem?> GetAsync(string filePath, CancellationToken cancellationToken = default) =>
        GetStorageItemInternalAsync(filePath, cancellationToken);


    public async Task<bool> IsExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var result = await GetStorageItemInternalAsync(filePath, cancellationToken);
        return result != null;
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        if (cache != null)
        {
            await cache.ClearAsync(cancellationToken);
        }

        await DoDeleteAllAsync(cancellationToken);
        if (MetadataProvider != null)
        {
            await MetadataProvider.DeleteAllMetadataAsync(cancellationToken);
        }
    }


    public Task<IEnumerable<StorageNode>> GetDirectoryContentsAsync(string path,
        CancellationToken cancellationToken = default)
    {
        if (MetadataProvider != null)
        {
            return MetadataProvider.GetDirectoryContentAsync(path, cancellationToken);
        }

        throw new InvalidOperationException("No metadata provider");
    }

    public async Task<IEnumerable<StorageNode>> RefreshDirectoryContentsAsync(string path,
        CancellationToken cancellationToken = default)
    {
        if (MetadataProvider != null)
        {
            var storageItems = await GetAllItemsAsync(path, cancellationToken);
            await MetadataProvider.RefreshDirectoryContentsAsync(storageItems, cancellationToken);
            return await MetadataProvider.GetDirectoryContentAsync(path, cancellationToken);
        }

        throw new InvalidOperationException("No metadata provider");
    }


    public Uri PublicUri(StorageItem item) => PublicUri(item.FilePath);

    public virtual Uri PublicUri(string filePath) => new(Options.PublicUri!, filePath);

    public bool IsDefault => Options.IsDefault;

    Task<IEnumerable<StorageItemInfo>> IStorage.GetAllItemsAsync(string path,
        CancellationToken cancellationToken) =>
        GetAllItemsAsync(path, cancellationToken);


    private async Task<StorageItem> SaveStorageItemAsync(UploadRequest request,
        CancellationToken cancellationToken = default)
    {
        request.Stream.Seek(0, SeekOrigin.Begin);
        var storageItem = await DoSaveAsync(request, cancellationToken);
        Logger.LogInformation("File saved to {Path}", request.Path);
        if (cache != null && !string.IsNullOrEmpty(storageItem.FilePath))
        {
            await cache.RemoveItemAsync(storageItem.FilePath, cancellationToken);
        }

        return storageItem;
    }

    protected virtual string GetDestinationPath(UploadRequest request)
    {
        var destinationName = GetStorageFileName(request.FileName);
        var path = request.Path;
        if (!string.IsNullOrEmpty(Options.Prefix))
        {
            path = Path.Combine(Options.Prefix, path);
        }

        return Helpers.PreparePath(Path.Combine(path, destinationName))!;
    }

    protected abstract Task<StorageItem> DoSaveAsync(UploadRequest uploadRequest,
        CancellationToken cancellationToken = default);

    protected abstract Task<bool> DoDeleteAsync(string filePath, CancellationToken cancellationToken = default);

    protected abstract Task<bool>
        DoIsFileExistsAsync(StorageItem item, CancellationToken cancellationToken = default);

    protected abstract Task DoDeleteAllAsync(CancellationToken cancellationToken = default);

    protected abstract Task<StorageItemDownloadInfo?> DoGetFileAsync(string path,
        CancellationToken cancellationToken = default);


    protected string GetPathWithPrefix(string filePath)
    {
        if (!string.IsNullOrEmpty(Options.Prefix) &&
            !filePath.StartsWith(Options.Prefix, StringComparison.InvariantCulture))
        {
            filePath = Helpers.PreparePath(Path.Combine(Options.Prefix, filePath))!;
        }

        return Helpers.PreparePath(filePath)!;
    }

    private async Task<StorageItemDownloadInfo?> GetStorageItemInfoAsync(string path,
        CancellationToken cancellationToken = default)
    {
        StorageItemDownloadInfo? result;
        if (cache != null)
        {
            result = await cache.GetOrAddItemAsync(path,
                async () => await DoGetFileAsync(GetPathWithPrefix(path), cancellationToken), cancellationToken);
        }
        else
        {
            result = await DoGetFileAsync(GetPathWithPrefix(path), cancellationToken);
            if (result is not null && MetadataProvider is not null)
            {
                var metadata = await MetadataProvider.GetMetadataAsync(path, cancellationToken);
                if (metadata is not null)
                {
                    result.SetMetadata(metadata);
                }
            }
        }

        return result;
    }

    private async Task<StorageItem?> GetStorageItemInternalAsync(string path,
        CancellationToken cancellationToken = default)
    {
        var result = await GetStorageItemInfoAsync(path, cancellationToken);

        return result?.StorageItem;
    }

    protected abstract Task<IEnumerable<StorageItemInfo>> GetAllItemsAsync(string path,
        CancellationToken cancellationToken = default);

    private string GetStorageFileName(string fileName)
    {
        if (Options.PreserveOriginalFileName)
        {
            return fileName;
        }

        var extension = fileName[fileName.LastIndexOf('.')..];
        return Guid.NewGuid() + extension;
    }
}
