namespace Sitko.Core.Storage;

/// <summary>
///     Download file result with StorageItem and Stream
/// </summary>
public record DownloadResult : IDisposable, IAsyncDisposable
{
    private bool isDisposed;

    public DownloadResult(StorageItem storageItem, Stream stream)
    {
        StorageItem = storageItem;
        Stream = stream;
    }

    /// <summary>
    ///     StorageItem with file info
    /// </summary>
    public StorageItem StorageItem { get; }

    /// <summary>
    ///     Stream with file data
    /// </summary>
    public Stream Stream { get; }

    public async ValueTask DisposeAsync()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            await Stream.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            Stream.Dispose();
            isDisposed = true;
        }

        GC.SuppressFinalize(this);
    }
}

