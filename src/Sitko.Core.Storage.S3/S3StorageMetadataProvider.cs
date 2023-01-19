using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.S3;

public class S3StorageMetadataProvider<TStorageOptions> : EmbedStorageMetadataProvider<S3Storage<TStorageOptions>,
    TStorageOptions, S3StorageMetadataModuleOptions<TStorageOptions>>
    where TStorageOptions : S3StorageOptions, new()
{
    public S3StorageMetadataProvider(IOptionsMonitor<S3StorageMetadataModuleOptions<TStorageOptions>> options,
        IOptionsMonitor<TStorageOptions> storageOptions,
        ILogger<S3StorageMetadataProvider<TStorageOptions>> logger) : base(options, storageOptions,
        logger)
    {
    }

    protected override async Task DoDeleteMetadataAsync(string filePath,
        CancellationToken cancellationToken = default)
    {
        if (await Storage.IsObjectExistsAsync(filePath, cancellationToken))
        {
            await Storage.DeleteObjectAsync(GetMetaDataPath(filePath),
                cancellationToken);
        }
    }

    protected override Task DoDeleteAllMetadataAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    protected override async Task DoSaveMetadataAsync(StorageItem storageItem, StorageItemMetadata? metadata = null,
        bool isNew = true,
        CancellationToken cancellationToken = default)
    {
        if (metadata is not null)
        {
            await Storage.DoSaveInternalAsync(GetMetaDataPath(storageItem.FilePath),
                new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metadata))), cancellationToken);
        }
    }

    protected override async Task<StorageItemMetadata?> DoGetMetadataJsonAsync(string path,
        CancellationToken cancellationToken = default)
    {
        var metaDataResponse =
            await Storage.DownloadFileAsync(GetMetaDataPath(path), cancellationToken);
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

public class S3StorageMetadataModuleOptions<TStorageOptions> : EmbedStorageMetadataModuleOptions<TStorageOptions>
    where TStorageOptions : StorageOptions
{
}

