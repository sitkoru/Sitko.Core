using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.Remote;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddRemoteStorage<TStorageOptions>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, TStorageOptions> configure, string? optionsKey = null)
        where TStorageOptions : StorageOptions, IRemoteStorageOptions, new()
    {
        hostApplicationBuilder.GetSitkoCore().AddRemoteStorage(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddRemoteStorage<TStorageOptions>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<TStorageOptions>? configure = null, string? optionsKey = null)
        where TStorageOptions : StorageOptions, IRemoteStorageOptions, new()
    {
        hostApplicationBuilder.GetSitkoCore().AddRemoteStorage(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddRemoteStorage<TStorageOptions>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, TStorageOptions> configure, string? optionsKey = null)
        where TStorageOptions : StorageOptions, IRemoteStorageOptions, new() =>
        applicationBuilder.AddModule<RemoteStorageModule<TStorageOptions>, TStorageOptions>(configure,
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddRemoteStorage<TStorageOptions>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<TStorageOptions>? configure = null, string? optionsKey = null)
        where TStorageOptions : StorageOptions, IRemoteStorageOptions, new() =>
        applicationBuilder.AddModule<RemoteStorageModule<TStorageOptions>, TStorageOptions>(configure,
            optionsKey);
}
