using System.Runtime.CompilerServices;
using Sitko.Core.App;
using Sitko.Core.Storage.Internal;

[assembly: InternalsVisibleTo("Sitko.Core.Storage.Metadata.Postgres.Tests")]

namespace Sitko.Core.Storage.Metadata;

// Generic parameter is required for dependency injection
// ReSharper disable once UnusedTypeParameter
public interface IStorageMetadataProvider<TStorageOptions> where TStorageOptions : StorageOptions
{
    internal Task InitAsync(CancellationToken cancellationToken = default);

    internal Task SaveMetadataAsync(StorageItem storageItem, StorageItemMetadata itemMetadata, bool isNew,
        CancellationToken cancellationToken = default);

    internal Task DeleteMetadataAsync(string filePath, CancellationToken cancellationToken = default);
    internal Task DeleteAllMetadataAsync(CancellationToken cancellationToken = default);

    internal Task<IEnumerable<StorageNode>> GetDirectoryContentAsync(string path,
        CancellationToken cancellationToken = default);

    internal Task RefreshDirectoryContentsAsync(IEnumerable<StorageItemInfo> storageItems,
        CancellationToken cancellationToken = default);

    public Task<StorageItemMetadata?>
        GetMetadataAsync(string path, CancellationToken cancellationToken = default);
}

// Generic interface is required for dependency injection
// ReSharper disable once UnusedTypeParameter
public interface IStorageMetadataProvider<TStorageOptions, TOptions> : IStorageMetadataProvider<TStorageOptions>
    where TStorageOptions : StorageOptions;

public interface IEmbedStorageMetadataProvider
{
    void SetStorage(IStorage currentStorage);
}

// ReSharper disable once UnusedTypeParameter
public abstract class StorageMetadataModuleOptions<TStorageOptions> : BaseModuleOptions
    where TStorageOptions : StorageOptions;
