using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Sitko.Core.Storage.Proxy.StaticFiles
{
    public class StorageFileProvider<TStorageOptions> : IFileProvider where TStorageOptions : StorageOptions
    {
        private readonly IStorage<TStorageOptions> _storage;
        private readonly ILogger<StorageFileProvider<TStorageOptions>> _logger;

        private readonly IFileInfo _empty = new PhysicalFileInfo(new FileInfo(Guid.NewGuid().ToString()));

        public StorageFileProvider(IStorage<TStorageOptions> storage,
            ILogger<StorageFileProvider<TStorageOptions>> logger)
        {
            _storage = storage;
            _logger = logger;
        }

        public IFileInfo GetFileInfo(string path)
        {
            var item = new StorageItem {FilePath = path};
            if (_storage.IsFileExistsAsync(item).GetAwaiter().GetResult())
            {
                return _storage.GetFileInfoAsync(item).GetAwaiter().GetResult();
            }

            _logger.LogWarning("File {Path} doesn't exists", path);
            return _empty;
        }

        public IDirectoryContents GetDirectoryContents(string path)
        {
            var content = _storage.GetDirectoryContentsAsync(path).GetAwaiter().GetResult();
            return content;
        }

        public IChangeToken Watch(string filter)
        {
            throw new NotImplementedException();
        }
    }
}
