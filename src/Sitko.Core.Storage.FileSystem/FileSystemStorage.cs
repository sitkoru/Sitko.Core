using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Storage.FileSystem
{
    public sealed class FileSystemStorage<T> : Storage<T>, IDisposable where T : IFileSystemStorageOptions
    {
        private readonly string _storagePath;

        public FileSystemStorage(T options, ILogger<FileSystemStorage<T>> logger) : base(options, logger)
        {
            _storagePath = options.StoragePath;
        }

        protected override async Task<bool> DoSaveAsync(string path, Stream file)
        {
            var dirPath = Path.Combine(_storagePath, Path.GetDirectoryName(path));
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath ?? throw new Exception($"Empty dir path in {path}"));
            }

            using var fileStream = File.Create(path);
            file.Seek(0, SeekOrigin.Begin);
            await file.CopyToAsync(fileStream);
            return true;
        }

        public override Task<bool> DeleteFileAsync(string filePath)
        {
            File.Delete(filePath);
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public interface IFileSystemStorageOptions : IStorageOptions
    {
        string StoragePath { get; }
    }
}
