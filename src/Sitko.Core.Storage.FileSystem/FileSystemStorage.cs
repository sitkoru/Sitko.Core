using System;
using System.IO;
using System.Text;
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

        protected override async Task<bool> DoSaveAsync(string path, Stream file,
            string metadata)
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

            var fullPath = Path.Combine(_storagePath, path);
            await using var fileStream = File.Create(fullPath);
            file.Seek(0, SeekOrigin.Begin);
            await file.CopyToAsync(fileStream);
            await using var metaDataStream = File.Create(GetMetaDataPath(fullPath));
            await metaDataStream.WriteAsync(Encoding.UTF8.GetBytes(metadata));
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
                    var metaDataPath = GetMetaDataPath(path);
                    if (File.Exists(metaDataPath))
                    {
                        File.Delete(metaDataPath);
                    }

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

        internal override async Task<StorageItemInfo?> DoGetFileAsync(string path)
        {
            StorageItemInfo? result = null;
            var fullPath = Path.Combine(_storagePath, path);
            var metaDataPath = GetMetaDataPath(fullPath);
            var fileInfo = new FileInfo(fullPath);
            var metaDataInfo = new FileInfo(metaDataPath);

            if (fileInfo.Exists)
            {
                string? metadata = null;
                if (metaDataInfo.Exists)
                {
                    metadata = await File.ReadAllTextAsync(metaDataPath);
                }

                result = new StorageItemInfo(metadata, fileInfo.Length, fileInfo.LastWriteTimeUtc,
                    () => new FileStream(fullPath, FileMode.Open));
            }


            return result;
        }

        protected override async Task<StorageNode?> DoBuildStorageTreeAsync()
        {
            var root = StorageNode.CreateDirectory("/", "/");
            await ListFolderAsync(root, "/");
            return root;
        }

        private async Task ListFolderAsync(StorageNode root, string path)
        {
            var fullPath = path == "/" ? _storagePath : Path.Combine(_storagePath, path.Trim('/'));
            if (Directory.Exists(fullPath))
            {
                foreach (var info in new DirectoryInfo(fullPath)
                    .EnumerateFileSystemInfos())
                {
                    if (info is DirectoryInfo dir)
                    {
                        await ListFolderAsync(root, PreparePath(Path.Combine(path, dir.Name))!);
                    }

                    if (info is FileInfo file)
                    {
                        if (file.Extension == MetaDataExtension)
                        {
                            continue;
                        }

                        string? metadata = null;
                        var metadataPath = GetMetaDataPath(file.FullName);
                        if (File.Exists(metadataPath))
                        {
                            metadata = await File.ReadAllTextAsync(metadataPath);
                        }

                        var item = CreateStorageItem(PreparePath(Path.Combine(path, file.Name))!.Trim('/'),
                            file.LastWriteTimeUtc,
                            file.Length,
                            metadata);

                        root.AddItem(item);
                    }
                }
            }
        }
    }
}
