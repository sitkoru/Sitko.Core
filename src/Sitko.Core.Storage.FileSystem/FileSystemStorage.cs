using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Storage.Cache;

namespace Sitko.Core.Storage.FileSystem
{
    public sealed class FileSystemStorage<T> : Storage<T> where T : StorageOptions, IFileSystemStorageOptions
    {
        private readonly string _storagePath;

        public FileSystemStorage(T options, ILogger<FileSystemStorage<T>> logger, IStorageCache? cache = null) : base(
            options, logger, cache)
        {
            _storagePath = options.StoragePath;
        }

        protected override async Task<bool> DoSaveAsync(string path, Stream file)
        {
            var dirName = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dirName))
            {
                return false;
            }

            var dirPath = Path.Combine(_storagePath, dirName);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            using var fileStream = File.Create(Path.Combine(_storagePath, path));
            file.Seek(0, SeekOrigin.Begin);
            await file.CopyToAsync(fileStream);
            return true;
        }

        protected override Task<bool> DoDeleteAsync(string filePath)
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

        protected override Task<bool> DoIsFileExistsAsync(StorageItem item)
        {
            var fullPath = Path.Combine(_storagePath, item.FilePath);
            return Task.FromResult(File.Exists(fullPath));
        }

        protected override Task DoDeleteAllAsync()
        {
            if (Directory.Exists(_storagePath))
            {
                Directory.Delete(_storagePath, true);
            }

            return Task.CompletedTask;
        }

        protected override Task<StorageItem?> DoGetFileInfoAsync(StorageItem item)
        {
            var fullPath = Path.Combine(_storagePath, item.FilePath);
            var fileInfo = new FileInfo(fullPath);
            if (!fileInfo.Exists)
            {
                return null;
            }

            item.LastModified = fileInfo.LastWriteTimeUtc;
            return Task.FromResult(item);
        }

        protected override Task<(StorageItem item, Stream stream)?> DoDownloadFileAsync(StorageItem item)
        {
            (StorageItem item, Stream stream)? result = null;
            var fullPath = Path.Combine(_storagePath, item.FilePath);
            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Exists)
            {
                item.LastModified = fileInfo.LastWriteTimeUtc;
                result = (item, fileInfo.OpenRead() as Stream);
            }

            return Task.FromResult(result);
        }

        public override Task<StorageItemCollection> GetDirectoryContentsAsync(string path)
        {
            var fullPath = Path.Combine(_storagePath, path);
            return Task.FromResult(new StorageItemCollection(GetFiles(fullPath)));
        }

        private List<StorageItem> GetFiles(string path)
        {
            return new DirectoryInfo(path)
                .EnumerateFileSystemInfos()
                .Select(info =>
                {
                    if (info is FileInfo file)
                    {
                        return new StorageItem
                        {
                            FileName = file.Name,
                            FileSize = file.Length,
                            LastModified = file.LastWriteTimeUtc,
                            Path = Path.GetDirectoryName(file.FullName),
                            FilePath = file.FullName
                        };
                    }

                    throw new InvalidOperationException("Unexpected type of FileSystemInfo");
                }).ToList();
        }
    }
}
