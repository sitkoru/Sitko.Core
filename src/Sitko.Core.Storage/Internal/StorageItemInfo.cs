using System;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Internal;

public record StorageItemInfo(string Path, long FileSize, DateTimeOffset Date, StorageItemMetadata? Metadata = null)
{
    public StorageItem GetStorageItem(StorageItemMetadata? storageItemMetadata = null) =>
        new(Path, storageItemMetadata ?? Metadata) { FileSize = FileSize, LastModified = Date };
}
