using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Internal;

public class StorageItemDownloadInfo
{
    private readonly string path;

    public StorageItemDownloadInfo(string path, long fileSize, DateTimeOffset date, Func<Task<Stream>> getStreamAsync)
    {
        this.path = path;
        FileSize = fileSize;
        Date = date;
        GetStreamAsync = getStreamAsync;
        Metadata = null;
    }

    public Func<Task<Stream>> GetStreamAsync { get; }

    public StorageItemMetadata? Metadata { get; private set; }

    public long FileSize { get; }
    public DateTimeOffset Date { get; }

    public StorageItem StorageItem => new(path, Metadata) { FileSize = FileSize, LastModified = Date };

    public void SetMetadata(StorageItemMetadata metadata) => Metadata = metadata;
}

