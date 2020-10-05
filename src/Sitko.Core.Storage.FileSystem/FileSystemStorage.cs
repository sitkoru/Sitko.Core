using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
            StorageItemMetadata metadata)
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
            var metaDataPath = fullPath + ".metadata";
            await using var fileStream = File.Create(fullPath);
            file.Seek(0, SeekOrigin.Begin);
            await file.CopyToAsync(fileStream);
            await using var metaDataStream = File.Create(metaDataPath);
            await metaDataStream.WriteAsync(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metadata.Metadata)));
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
                    var metaDataPath = path + ".metadata";
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

        protected override async Task<FileDownloadResult?> DoGetFileAsync(string path)
        {
            FileDownloadResult? result = null;
            var fullPath = Path.Combine(_storagePath, path);
            var metaDataPath = fullPath + ".metadata";
            var fileInfo = new FileInfo(fullPath);
            var metaDataInfo = new FileInfo(metaDataPath);

            if (fileInfo.Exists)
            {
                StorageItemMetadata metadata;
                if (metaDataInfo.Exists)
                {
                    var json = await File.ReadAllTextAsync(metaDataPath);
                    metadata = new StorageItemMetadata(
                        JsonSerializer.Deserialize<List<KeyValuePair<string, string>>>(json));
                }
                else
                {
                    metadata = new StorageItemMetadata().Add(StorageItemMetadata.FieldFileName, fileInfo.Name)
                        .Add(StorageItemMetadata.FieldDate, fileInfo.LastWriteTimeUtc.ToString("O"));
                }

                result = new FileDownloadResult(metadata, fileInfo.Length, fileInfo.OpenRead());
            }


            return result;
        }

        protected override Task<StorageFolder?> DoBuildStorageTreeAsync()
        {
            return Task.FromResult(ListFolder("/"));
        }

        private StorageFolder ListFolder(string path)
        {
            var fullPath = path == "/" ? _storagePath : Path.Combine(_storagePath, path.Trim('/'));
            IEnumerable<IStorageNode>? children = null;
            if (Directory.Exists(fullPath))
            {
                children = new DirectoryInfo(fullPath)
                    .EnumerateFileSystemInfos()
                    .Select(info =>
                    {
                        return info switch
                        {
                            FileInfo metaData when metaData.Extension == ".metadata" => null,
                            FileInfo file => new StorageItem
                            {
                                FileName = file.Name,
                                FileSize = file.Length,
                                LastModified = file.LastWriteTimeUtc,
                                Path = PreparePath(path)!.Trim('/'),
                                FilePath = PreparePath(Path.Combine(path, file.Name))!.Trim('/')
                            } as IStorageNode,
                            DirectoryInfo dir => ListFolder(PreparePath(Path.Combine(path, dir.Name))),
                            _ => throw new InvalidOperationException("Unexpected type of FileSystemInfo")
                        };
                    }).Where(c => c != null);
            }

            return new StorageFolder(path == "/" ? "/" : Path.GetFileNameWithoutExtension(path),
                PreparePath(Path.Combine(_storagePath, path)),
                children);
        }
    }
}
