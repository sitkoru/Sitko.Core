using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Sitko.Core.App;

[assembly: InternalsVisibleTo("Sitko.Core.Storage.Metadata.Postgres.Tests")]

namespace Sitko.Core.Storage.Metadata
{
    // Generic parameter is required for dependency injection
    // ReSharper disable once UnusedTypeParameter
    public interface IStorageMetadataProvider<TStorageOptions> : IAsyncDisposable where TStorageOptions : StorageOptions
    {
        internal Task InitAsync();

        internal Task SaveMetadataAsync(StorageItem storageItem, StorageItemMetadata itemMetadata,
            CancellationToken cancellationToken = default);

        internal Task DeleteMetadataAsync(string filePath, CancellationToken cancellationToken = default);
        internal Task DeleteAllMetadataAsync(CancellationToken cancellationToken = default);

        internal Task<IEnumerable<StorageNode>> GetDirectoryContentAsync(string path,
            CancellationToken cancellationToken = default);

        internal Task RefreshDirectoryContentsAsync(IEnumerable<StorageItemInfo> storageItems,
            CancellationToken cancellationToken = default);

        internal Task<StorageItemMetadata?>
            GetMetadataAsync(string path, CancellationToken cancellationToken = default);
    }

    // Generic interface is required for dependency injection
    // ReSharper disable once UnusedTypeParameter
    public interface IStorageMetadataProvider<TStorageOptions, TOptions> : IStorageMetadataProvider<TStorageOptions>
        where TStorageOptions : StorageOptions
    {
    }

    public abstract class StorageMetadataProviderOptions : BaseModuleOptions
    {
    }
}
