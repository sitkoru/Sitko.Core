using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.FileSystem
{
    public class FileSystemStorageMetadataProvider<TFileSystemOptions> : EmbedStorageMetadataProvider<
        FileSystemStorage<TFileSystemOptions>,
        TFileSystemOptions, FileSystemStorageMetadataProviderOptions>
        where TFileSystemOptions : StorageOptions, IFileSystemStorageOptions

    {
        public FileSystemStorageMetadataProvider(IServiceProvider serviceProvider, TFileSystemOptions storageOptions,
            FileSystemStorageMetadataProviderOptions options,
            ILogger<FileSystemStorageMetadataProvider<TFileSystemOptions>> logger) : base(serviceProvider, options,
            storageOptions,
            logger)
        {
        }

        protected override Task DoDeleteMetadataAsync(string filePath, CancellationToken? cancellationToken)
        {
            var fullPath = Path.Combine(StorageOptions.StoragePath, filePath);
            var metaDataPath = GetMetaDataPath(fullPath);
            if (File.Exists(metaDataPath))
            {
                File.Delete(metaDataPath);
            }

            return Task.CompletedTask;
        }

        protected override Task DoDeleteAllMetadataAsync(CancellationToken? cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override async Task<StorageItemMetadata?> DoGetMetadataJsonAsync(string path,
            CancellationToken? cancellationToken = null)
        {
            var fullPath = Path.Combine(StorageOptions.StoragePath, path);
            var metaDataPath = GetMetaDataPath(fullPath);
            var metaDataInfo = new FileInfo(metaDataPath);
            if (metaDataInfo.Exists)
            {
                var json = await File.ReadAllTextAsync(metaDataPath, cancellationToken ?? CancellationToken.None);
                if (!string.IsNullOrEmpty(json))
                {
                    return JsonSerializer.Deserialize<StorageItemMetadata>(json);
                }
            }

            return null;
        }

        protected override async Task DoSaveMetadataAsync(StorageItem storageItem, StorageItemMetadata? metadata = null,
            CancellationToken? cancellationToken = null)
        {
            if (metadata is not null)
            {
                var fullPath = Path.Combine(StorageOptions.StoragePath, storageItem.FilePath);
                await using var metaDataStream = File.Create(GetMetaDataPath(fullPath));
                await metaDataStream.WriteAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metadata)),
                    cancellationToken ?? CancellationToken.None);
            }
        }
    }

    public class FileSystemStorageMetadataProviderOptions : EmbedStorageMetadataProviderOptions
    {
    }
}
