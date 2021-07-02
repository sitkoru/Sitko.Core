using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;

namespace Sitko.Core.Storage.Metadata
{
    public abstract class
        EmbedStorageMetadataProvider<TStorage, TStorageOptions, TOptions> : BaseStorageMetadataProvider<TOptions,
            TStorageOptions>
        where TStorage : IStorage<TStorageOptions>
        where TOptions : EmbedStorageMetadataProviderOptions
        where TStorageOptions : StorageOptions
    {
        private readonly IServiceProvider _serviceProvider;
        private TStorage? _storage;

        protected TStorage Storage
        {
            get
            {
                if (_storage is null)
                {
                    _storage = _serviceProvider.GetRequiredService<TStorage>();
                }

                return _storage;
            }
        }

        private StorageNode? _tree;
        private DateTimeOffset? _treeLastBuild;
        private readonly AsyncLock _treeLock = new();

        protected const string MetaDataExtension = ".metadata";

        protected string GetMetaDataPath(string filePath)
        {
            filePath = filePath + MetaDataExtension;
            if (!string.IsNullOrEmpty(StorageOptions.CurrentValue.Prefix) &&
                !filePath.StartsWith(StorageOptions.CurrentValue.Prefix))
            {
                filePath = Helpers.PreparePath($"{StorageOptions.CurrentValue.Prefix}/{filePath}")!;
            }

            return filePath;
        }

        protected EmbedStorageMetadataProvider(IServiceProvider serviceProvider, IOptionsMonitor<TOptions> options,
            IOptionsMonitor<TStorageOptions> storageOptions,
            ILogger<EmbedStorageMetadataProvider<TStorage, TStorageOptions, TOptions>> logger)
            : base(options, storageOptions, logger)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task<IEnumerable<StorageNode>> DoGetDirectoryContentsAsync(string path,
            CancellationToken? cancellationToken = null)
        {
            if (_tree == null || _treeLastBuild <
                DateTimeOffset.UtcNow.Subtract(Options.CurrentValue.StorageTreeCacheTimeout))
            {
                await BuildStorageTreeAsync(cancellationToken);
            }

            if (_tree == null) { return new List<StorageNode>(); }

            var parts = Helpers.PreparePath(path.Trim('/'))!.Split("/");
            var current = _tree;
            foreach (var part in parts)
            {
                current = current?.Children.Where(n => n.Type == StorageNodeType.Directory)
                    .FirstOrDefault(f => f.Name == part);
            }

            return current?.Children ?? new StorageNode[0];
        }

        private TaskCompletionSource<bool>? _treeBuildTaskSource;

        private async Task BuildStorageTreeAsync(CancellationToken? cancellationToken = null)
        {
            if (_treeBuildTaskSource != null)
            {
                await _treeBuildTaskSource.Task;
                return;
            }

            using (await _treeLock.LockAsync(cancellationToken ?? CancellationToken.None))
            {
                Logger.LogInformation("Start building storage tree");
                _treeBuildTaskSource = new TaskCompletionSource<bool>();
                _tree = StorageNode.CreateDirectory("/", "/");
                var items = await Storage.GetAllItemsAsync("/", cancellationToken);
                foreach (var info in items)
                {
                    if (info.Path.EndsWith(MetaDataExtension))
                    {
                        continue;
                    }

                    var metadata = await DoGetMetadataAsync(info.Path, cancellationToken);
                    var item = new StorageItem(info, StorageOptions.CurrentValue.Prefix, metadata);

                    _tree.AddItem(item);
                }

                _treeLastBuild = DateTimeOffset.UtcNow;
                _treeBuildTaskSource.SetResult(true);
                _treeBuildTaskSource = null;
                Logger.LogInformation("Done building storage tree");
            }
        }

        public override ValueTask DisposeAsync()
        {
            return new();
        }
    }

    public class EmbedStorageMetadataProviderOptions : StorageMetadataProviderOptions
    {
        public TimeSpan StorageTreeCacheTimeout { get; set; } = TimeSpan.FromMinutes(30);
    }
}
