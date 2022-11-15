using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Internal;

public record UploadRequest(Stream Stream, string Path, string FileName,
    StorageItemMetadata? Metadata = null)
{
    public long FileSize { get; } = Stream.Length;

    public StorageItem GetStorageItem(string destinationPath) => new(destinationPath, Metadata)
    {
        FileSize = FileSize, LastModified = DateTimeOffset.UtcNow
    };
}

