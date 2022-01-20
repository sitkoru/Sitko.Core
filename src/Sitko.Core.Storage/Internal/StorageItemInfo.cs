using System;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Internal;

public record StorageItemInfo(string Path, long FileSize, DateTimeOffset Date)
{
    public StorageItem GetStorageItem(StorageItemMetadata? storageItemMetadata = null) =>
        new(Path, storageItemMetadata) { FileSize = FileSize, LastModified = Date };
}
