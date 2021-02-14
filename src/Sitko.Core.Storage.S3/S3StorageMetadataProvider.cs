using System;
using System.IO;
using System.Text;
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
            ILogger<S3StorageMetadataProvider<TS3Options>> logger) : base(serviceProvider, options, logger)
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

        protected override async Task DoSaveMetadataAsync(StorageItem storageItem, string? metadata = null,
            CancellationToken? cancellationToken = null)
        {
            if (metadata is not null)
            {
                await Storage.DoSaveInternalAsync(GetMetaDataPath(storageItem.FilePath),
                    new MemoryStream(Encoding.UTF8.GetBytes(metadata)), cancellationToken);
            }
        }

        protected override async Task<string?> DoGetMetadataJsonAsync(string filePath,
            CancellationToken? cancellationToken = null)
        {
            string? metaData = null;
            var metaDataResponse =
                await Storage.DownloadFileAsync(GetMetaDataPath(filePath), cancellationToken);
            if (metaDataResponse != null)
            {
                metaData = await metaDataResponse.ResponseStream.DownloadStreamAsString(cancellationToken);
            }

            return metaData;
        }
    }

    internal class S3StorageMetadataProviderOptions : EmbedStorageMetadataProviderOptions
    {
    }
}
