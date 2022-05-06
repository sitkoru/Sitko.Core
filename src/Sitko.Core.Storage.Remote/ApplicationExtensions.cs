using System;
using Sitko.Core.App;

namespace Sitko.Core.Storage.Remote;

public static class ApplicationExtensions
{
    public static Application AddRemoteStorage<TStorageOptions>(this Application application,
        Action<IApplicationContext, TStorageOptions> configure, string? optionsKey = null)
        where TStorageOptions : StorageOptions, IRemoteStorageOptions, new() =>
        application.AddModule<RemoteStorageModule<TStorageOptions>, TStorageOptions>(configure,
            optionsKey);

    public static Application AddRemoteStorage<TStorageOptions>(this Application application,
        Action<TStorageOptions>? configure = null, string? optionsKey = null)
        where TStorageOptions : StorageOptions, IRemoteStorageOptions, new() =>
        application.AddModule<RemoteStorageModule<TStorageOptions>, TStorageOptions>(configure,
            optionsKey);
}
