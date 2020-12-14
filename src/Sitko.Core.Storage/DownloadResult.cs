using System;
using System.IO;
using System.Threading.Tasks;

namespace Sitko.Core.Storage
{
    public record DownloadResult(StorageItem StorageItem, Stream Stream) : IDisposable, IAsyncDisposable
    {
        private bool _isDisposed;

        public async ValueTask DisposeAsync()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                await Stream.DisposeAsync();
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Stream.Dispose();
                _isDisposed = true;
            }
        }
    };
}
