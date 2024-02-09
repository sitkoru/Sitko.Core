using System.Collections.Concurrent;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Sitko.Core.Storage.Internal;

namespace Sitko.Core.Storage.Metadata;

public abstract class
    EmbedStorageMetadataProvider<TStorage, TStorageOptions, TOptions> : BaseStorageMetadataProvider<TOptions,
    TStorageOptions>, IEmbedStorageMetadataProvider
    where TStorage : IStorage<TStorageOptions>
    where TOptions : EmbedStorageMetadataModuleOptions<TStorageOptions>
    where TStorageOptions : StorageOptions
{
    [PublicAPI] protected const string MetaDataExtension = ".metadata";

    //private readonly IServiceProvider serviceProvider;
    private readonly AsyncLock treeLock = new();
    private TStorage? storage;

    private StorageNode? tree;

    private TaskCompletionSource<bool>? treeBuildTaskSource;
    private DateTimeOffset? treeLastBuild;

    protected EmbedStorageMetadataProvider(IOptionsMonitor<TOptions> options,
        IOptionsMonitor<TStorageOptions> storageOptions,
        ILogger<EmbedStorageMetadataProvider<TStorage, TStorageOptions, TOptions>> logger)
        : base(options, storageOptions, logger)
    {
    }

    protected TStorage Storage
    {
        get
        {
            if (storage is null)
            {
                throw new InvalidOperationException("Set Storage via SetStorage call");
            }

            return storage;
        }
    }

    public void SetStorage(IStorage currentStorage)
    {
        if (storage is not null)
        {
            return;
        }

        if (currentStorage is not TStorage typedStorage)
        {
            throw new InvalidOperationException("Incorrect storage type");
        }

        storage = typedStorage;
    }

    protected string GetMetaDataPath(string filePath)
    {
        filePath += MetaDataExtension;
        if (!string.IsNullOrEmpty(StorageOptions.CurrentValue.Prefix) &&
            !filePath.StartsWith(StorageOptions.CurrentValue.Prefix, StringComparison.InvariantCulture))
        {
            filePath = Helpers.PreparePath($"{StorageOptions.CurrentValue.Prefix}/{filePath}")!;
        }

        return filePath;
    }

    protected override async Task<IEnumerable<StorageNode>> DoGetDirectoryContentsAsync(string path,
        CancellationToken cancellationToken = default)
    {
        if (tree == null || treeLastBuild <
            DateTimeOffset.UtcNow.Subtract(
                TimeSpan.FromMinutes(Options.CurrentValue.StorageTreeCacheTimeoutInMinutes)))
        {
            await BuildStorageTreeAsync(cancellationToken);
        }

        if (tree == null)
        {
            return new List<StorageNode>();
        }

        var parts = Helpers.PreparePath(path.Trim('/'))!.Split("/");
        var current = tree;
        foreach (var part in parts)
        {
            current = current?.Children.Where(n => n.Type == StorageNodeType.Directory)
                .FirstOrDefault(f => f.Name == part);
        }

        return current?.Children ?? Array.Empty<StorageNode>();
    }

    private async Task BuildStorageTreeAsync(CancellationToken cancellationToken = default)
    {
        if (treeBuildTaskSource != null)
        {
            await treeBuildTaskSource.Task;
            return;
        }

        using (await treeLock.LockAsync(cancellationToken))
        {
            Logger.LogInformation("Start building storage tree");
            treeBuildTaskSource = new TaskCompletionSource<bool>();
            tree = StorageNode.CreateDirectory("/", "/");
            var items = (await Storage.GetAllItemsAsync("/", cancellationToken)).ToDictionary(info => info.Path,
                info => info);
            var metadataItems =
                items.Where(pair => pair.Key.EndsWith(MetaDataExtension, StringComparison.InvariantCulture))
                    .Select(pair => pair.Value).ToList();
            var allMetadata = new ConcurrentDictionary<string, StorageItemMetadata?>();
            Logger.LogInformation("Download {Count} metadata items", metadataItems.Count);
            await Parallel.ForEachAsync(metadataItems,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = Options.CurrentValue.MaxParallelDownloadStreams
                }, async (info, token) =>
                {
                    var filePath = info.Path.Replace(MetaDataExtension, "");
                    var metadata = await DoGetMetadataAsync(filePath, token);
                    allMetadata.TryAdd(filePath, metadata);
                });
            Logger.LogInformation("Metadata downloaded");
            foreach (var (path, info) in items.Where(pair =>
                         !pair.Key.EndsWith(MetaDataExtension, StringComparison.InvariantCulture)))
            {
                StorageItemMetadata? metadata = null;
                if (allMetadata.TryGetValue(path, out var itemMetadata) && itemMetadata is not null)
                {
                    metadata = itemMetadata;
                }

                var item = info.GetStorageItem(metadata);

                tree.AddItem(item);
            }

            treeLastBuild = DateTimeOffset.UtcNow;
            treeBuildTaskSource.SetResult(true);
            treeBuildTaskSource = null;
            Logger.LogInformation("Done building storage tree");
        }
    }


    protected sealed override async Task DoDeleteMetadataAsync(string filePath,
        CancellationToken cancellationToken = default)
    {
        await DeleteEmbededMetadataAsync(filePath, cancellationToken);
        tree?.RemoveItem(filePath);
    }

    protected sealed override async Task DoSaveMetadataAsync(StorageItem storageItem,
        StorageItemMetadata? metadata = null,
        bool isNew = true,
        CancellationToken cancellationToken = default)
    {
        await SaveEmbededMetadataAsync(storageItem, metadata, isNew, cancellationToken);
        tree?.AddOrUpdateItem(storageItem);
    }

    protected sealed override async Task DoDeleteAllMetadataAsync(CancellationToken cancellationToken = default)
    {
        await DeleteAllEmbededMetadataAsync(cancellationToken);
        tree = null;
    }

    protected abstract Task DeleteEmbededMetadataAsync(string filePath, CancellationToken cancellationToken = default);

    protected abstract Task DeleteAllEmbededMetadataAsync(CancellationToken cancellationToken = default);

    protected abstract Task SaveEmbededMetadataAsync(StorageItem storageItem, StorageItemMetadata? metadata = null,
        bool isNew = true,
        CancellationToken cancellationToken = default);
}

public class EmbedStorageMetadataModuleOptions<TStorageOptions> : StorageMetadataModuleOptions<TStorageOptions>
    where TStorageOptions : StorageOptions
{
    public int StorageTreeCacheTimeoutInMinutes { get; set; } = 30;
    public int MaxParallelDownloadStreams { get; set; } = 8;
}
