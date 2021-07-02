using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.S3
{
    public class S3StorageMetadataProvider<TStorageOptions> : EmbedStorageMetadataProvider<S3Storage<TStorageOptions>,
        TStorageOptions, S3StorageMetadataProviderOptions>
        where TStorageOptions : S3StorageOptions, new()
    {
        public S3StorageMetadataProvider(IServiceProvider serviceProvider,
            IOptionsMonitor<S3StorageMetadataProviderOptions> options,
            IOptionsMonitor<TStorageOptions> storageOptions,
            ILogger<S3StorageMetadataProvider<TStorageOptions>> logger) : base(serviceProvider, options, storageOptions,
            logger)
        {
        }

        protected override async Task DoDeleteMetadataAsync(string filePath,
            CancellationToken cancellationToken = default)
        {
            if (await Storage.IsObjectExistsAsync(filePath, cancellationToken))
                await Storage.DeleteObjectAsync(GetMetaDataPath(filePath),
                    cancellationToken);
        }

        protected override Task DoDeleteAllMetadataAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        protected override async Task DoSaveMetadataAsync(StorageItem storageItem, StorageItemMetadata? metadata = null,
            CancellationToken cancellationToken = default)
        {
            if (metadata is not null)
                await Storage.DoSaveInternalAsync(GetMetaDataPath(storageItem.FilePath),
                    new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metadata))), cancellationToken);
        }

        protected override async Task<StorageItemMetadata?> DoGetMetadataJsonAsync(string filePath,
            CancellationToken cancellationToken = default)
        {
            GetObjectResponse? metaDataResponse =
                await Storage.DownloadFileAsync(GetMetaDataPath(filePath), cancellationToken);
            if (metaDataResponse != null)
            {
                var json = await metaDataResponse.ResponseStream.DownloadStreamAsString(cancellationToken);
                if (!string.IsNullOrEmpty(json)) return JsonSerializer.Deserialize<StorageItemMetadata>(json);
            }

            return null;
        }
    }

    public class S3StorageMetadataProviderOptions : EmbedStorageMetadataProviderOptions
    {
    }
}
