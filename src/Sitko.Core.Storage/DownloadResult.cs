using System;
using System.IO;
using System.Threading.Tasks;

namespace Sitko.Core.Storage
{
    /// <summary>
    /// Download file result with StorageItem and Stream
    /// </summary>
    public record DownloadResult : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// StorageItem with file info
        /// </summary>
        public StorageItem StorageItem { get; }

        /// <summary>
        /// Stream with file data
        /// </summary>
        public Stream Stream { get; }

        private bool isDisposed;

        public DownloadResult(StorageItem storageItem, Stream stream)
        {
            StorageItem = storageItem;
            Stream = stream;
        }

        public async ValueTask DisposeAsync()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                await Stream.DisposeAsync();
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                Stream.Dispose();
                isDisposed = true;
            }
        }
    };
}
