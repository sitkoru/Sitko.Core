﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Remote;

public class RemoteStorageMetadataProvider<TStorageOptions> : EmbedStorageMetadataProvider<
    RemoteStorage<TStorageOptions>,
    TStorageOptions, RemoteStorageMetadataModuleOptions<TStorageOptions>>
    where TStorageOptions : StorageOptions, IRemoteStorageOptions, new()
{
    public RemoteStorageMetadataProvider(IOptionsMonitor<RemoteStorageMetadataModuleOptions<TStorageOptions>> options,
        IOptionsMonitor<TStorageOptions> storageOptions,
        ILogger<RemoteStorageMetadataProvider<TStorageOptions>> logger) : base(options,
        storageOptions, logger)
    {
    }

    protected override Task
        DoDeleteMetadataAsync(string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;

    protected override Task DoDeleteAllMetadataAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    protected override Task<StorageItemMetadata?> DoGetMetadataJsonAsync(string path,
        CancellationToken cancellationToken = default) => Task.FromResult<StorageItemMetadata?>(null);

    protected override Task DoSaveMetadataAsync(StorageItem storageItem, StorageItemMetadata? metadata = null,
        bool isNew = true,
        CancellationToken cancellationToken = default) => isNew
        ? Task.CompletedTask
        : metadata is null
            ? Task.CompletedTask
            : Storage.DoUpdateMetaDataAsync(storageItem, metadata, cancellationToken);
}

public class RemoteStorageMetadataModuleOptions<TStorageOptions> : EmbedStorageMetadataModuleOptions<TStorageOptions>
    where TStorageOptions : StorageOptions
{
}

