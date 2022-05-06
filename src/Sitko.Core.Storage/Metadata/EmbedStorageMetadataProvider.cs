using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            !filePath.StartsWith(StorageOptions.CurrentValue.Prefix))
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
            var items = await Storage.GetAllItemsAsync("/", cancellationToken);
            foreach (var info in items)
            {
                if (info.Path.EndsWith(MetaDataExtension))
                {
                    continue;
                }

                StorageItemMetadata? metadata = null;
                if (info.Metadata is null)
                {
                    metadata = await DoGetMetadataAsync(info.Path, cancellationToken);
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
}

public class EmbedStorageMetadataModuleOptions<TStorageOptions> : StorageMetadataModuleOptions<TStorageOptions>
    where TStorageOptions : StorageOptions
{
    public int StorageTreeCacheTimeoutInMinutes { get; set; } = 30;
}
