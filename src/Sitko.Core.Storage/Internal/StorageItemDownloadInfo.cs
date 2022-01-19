using System;
using System.IO;
using System.Threading.Tasks;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Internal;

public class StorageItemDownloadInfo
{
    public StorageItemDownloadInfo(long fileSize, DateTimeOffset date, Func<Task<Stream>> getStreamAsync)
    {
        FileSize = fileSize;
        Date = date;
        GetStreamAsync = getStreamAsync;
        Metadata = null;
    }

    public Func<Task<Stream>> GetStreamAsync { get; }

    public StorageItemMetadata? Metadata { get; private set; }

    public long FileSize { get; }
    public DateTimeOffset Date { get; }

    public void SetMetadata(StorageItemMetadata metadata) => Metadata = metadata;
}
