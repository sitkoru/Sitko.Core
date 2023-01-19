using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.Storage.ImgProxy;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddImgProxyStorage<TStorageOptions>(this Application application,
        Action<IApplicationContext, BaseApplicationModuleOptions> configure,
        string? optionsKey = null)
        where TStorageOptions : StorageOptions =>
        application
            .AddModule<ImgProxyStorageModule<TStorageOptions>, BaseApplicationModuleOptions>(
                configure, optionsKey);

    public static Application AddImgProxyStorage<TStorageOptions>(this Application application,
        Action<BaseApplicationModuleOptions>? configure = null,
        string? optionsKey = null)
        where TStorageOptions : StorageOptions =>
        application
            .AddModule<ImgProxyStorageModule<TStorageOptions>, BaseApplicationModuleOptions>(
                configure, optionsKey);
}

