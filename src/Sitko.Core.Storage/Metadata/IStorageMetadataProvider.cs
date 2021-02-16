using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sitko.Core.Storage.Metadata
{
    public interface IStorageMetadataProvider : IAsyncDisposable
    {
        internal Task InitAsync();
        internal Task SaveMetadataAsync(StorageItem storageItem, StorageItemMetadata itemMetadata,
            CancellationToken? cancellationToken = null);

        internal Task DeleteMetadataAsync(string filePath, CancellationToken? cancellationToken = null);
        internal Task DeleteAllMetadataAsync(CancellationToken? cancellationToken = null);

        internal Task<IEnumerable<StorageNode>> GetDirectoryContentAsync(string path,
            CancellationToken? cancellationToken = null);

        internal Task RefreshDirectoryContentsAsync(string path, IEnumerable<StorageItemInfo> storageItems,
            CancellationToken? cancellationToken = null);

        internal Task<StorageItemMetadata?> GetMetadataAsync(string path, CancellationToken? cancellationToken = null);
    }

    public interface IStorageMetadataProvider<TOptions> : IStorageMetadataProvider
        where TOptions : StorageMetadataProviderOptions
    {
    }

    public abstract class StorageMetadataProviderOptions
    {
    }
}
