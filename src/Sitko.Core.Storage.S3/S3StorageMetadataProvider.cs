using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.S3
{
    internal class S3StorageMetadataProvider<TS3Options> : EmbedStorageMetadataProvider<S3Storage<TS3Options>,
        TS3Options, S3StorageMetadataProviderOptions>
        where TS3Options : StorageOptions, IS3StorageOptions
    {
        public S3StorageMetadataProvider(IServiceProvider serviceProvider, S3StorageMetadataProviderOptions options,
            TS3Options storageOptions,
            ILogger<S3StorageMetadataProvider<TS3Options>> logger) : base(serviceProvider, options, storageOptions,
            logger)
        {
        }

        protected override async Task DoDeleteMetadataAsync(string filePath, CancellationToken? cancellationToken)
        {
            if (await Storage.IsObjectExistsAsync(filePath, cancellationToken))
            {
                await Storage.DeleteObjectAsync(GetMetaDataPath(filePath),
                    cancellationToken ?? CancellationToken.None);
            }
        }

        protected override Task DoDeleteAllMetadataAsync(CancellationToken? cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override async Task DoSaveMetadataAsync(StorageItem storageItem, StorageItemMetadata? metadata = null,
            CancellationToken? cancellationToken = null)
        {
            if (metadata is not null)
            {
                await Storage.DoSaveInternalAsync(GetMetaDataPath(storageItem.FilePath),
                    new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metadata))), cancellationToken);
            }
        }

        protected override async Task<StorageItemMetadata?> DoGetMetadataJsonAsync(string filePath,
            CancellationToken? cancellationToken = null)
        {
            var metaDataResponse =
                await Storage.DownloadFileAsync(GetMetaDataPath(filePath), cancellationToken);
            if (metaDataResponse != null)
            {
                var json = await metaDataResponse.ResponseStream.DownloadStreamAsString(cancellationToken);
                if (!string.IsNullOrEmpty(json))
                {
                    return JsonSerializer.Deserialize<StorageItemMetadata>(json);
                }
            }

            return null;
        }
    }

    internal class S3StorageMetadataProviderOptions : EmbedStorageMetadataProviderOptions
    {
    }
}
