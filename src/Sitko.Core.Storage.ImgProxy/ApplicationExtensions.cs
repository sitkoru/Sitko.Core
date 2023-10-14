using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.ImgProxy;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddImgProxyStorage<TStorageOptions>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, BaseApplicationModuleOptions> configure,
        string? optionsKey = null)
        where TStorageOptions : StorageOptions
    {
        hostApplicationBuilder.AddSitkoCore().AddImgProxyStorage<TStorageOptions>(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddImgProxyStorage<TStorageOptions>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<BaseApplicationModuleOptions>? configure = null,
        string? optionsKey = null)
        where TStorageOptions : StorageOptions
    {
        hostApplicationBuilder.AddSitkoCore().AddImgProxyStorage<TStorageOptions>(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddImgProxyStorage<TStorageOptions>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, BaseApplicationModuleOptions> configure,
        string? optionsKey = null)
        where TStorageOptions : StorageOptions =>
        applicationBuilder
            .AddModule<ImgProxyStorageModule<TStorageOptions>, BaseApplicationModuleOptions>(
                configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddImgProxyStorage<TStorageOptions>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<BaseApplicationModuleOptions>? configure = null,
        string? optionsKey = null)
        where TStorageOptions : StorageOptions =>
        applicationBuilder
            .AddModule<ImgProxyStorageModule<TStorageOptions>, BaseApplicationModuleOptions>(
                configure, optionsKey);
}
