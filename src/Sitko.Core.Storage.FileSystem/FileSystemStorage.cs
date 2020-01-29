using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Storage.FileSystem
{
    public sealed class FileSystemStorage<T> : Storage<T> where T : IFileSystemStorageOptions
    {
        private readonly string _storagePath;
        private readonly List<FileStream> _openedStreams = new List<FileStream>();

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

            using var fileStream = File.Create(Path.Combine(_storagePath, path));
            file.Seek(0, SeekOrigin.Begin);
            await file.CopyToAsync(fileStream);
            return true;
        }

        public override Task<bool> DeleteFileAsync(string filePath)
        {
            var path = Path.Combine(_storagePath, filePath);
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    return Task.FromResult(true);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while deleting file {File}: {ErrorText}", path, ex.ToString());
                }
            }

            return Task.FromResult(false);
        }

        public override Task<Stream> DownloadFileAsync(StorageItem item)
        {
            var stream = File.OpenRead(Path.Combine(_storagePath, item.FilePath));
            _openedStreams.Add(stream);
            return Task.FromResult((Stream)stream);
        }

        public override Task DeleteAllAsync()
        {
            if (Directory.Exists(_storagePath))
            {
                ClearStreams();
                Directory.Delete(_storagePath, true);
            }

            return Task.CompletedTask;
        }

        private void ClearStreams()
        {
            if (_openedStreams.Any())
            {
                foreach (FileStream stream in _openedStreams)
                {
                    stream.Close();
                }

                _openedStreams.Clear();
            }
        }

        public override ValueTask DisposeAsync()
        {
            ClearStreams();
            return base.DisposeAsync();
        }
    }

    public interface IFileSystemStorageOptions : IStorageOptions
    {
        string StoragePath { get; }
    }
}
