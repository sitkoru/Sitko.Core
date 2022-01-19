using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Storage.Cache;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Remote;

public class RemoteStorage<TStorageOptions> : Storage<TStorageOptions>
    where TStorageOptions : StorageOptions, IRemoteStorageOptions
{
    public RemoteStorage(IOptionsMonitor<TStorageOptions> options, ILogger<RemoteStorage<TStorageOptions>> logger,
        IStorageCache<TStorageOptions>? cache,
        IStorageMetadataProvider<TStorageOptions>? metadataProvider) : base(options, logger,
        cache, metadataProvider)
    {
    }

    protected override async Task<bool> DoSaveAsync(string path, Stream file,
        CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override async Task<bool> DoDeleteAsync(string filePath, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    protected override async Task<bool> DoIsFileExistsAsync(StorageItem item,
        CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override async Task DoDeleteAllAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
