using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.ImgProxy
{
    using JetBrains.Annotations;

    [PublicAPI]
    public static class ApplicationExtensions
    {
        public static Application AddImgProxyStorage<TStorageOptions>(this Application application,
            Action<IConfiguration, IHostEnvironment, ImgProxyStorageModuleOptions<TStorageOptions>> configure,
            string? optionsKey = null)
            where TStorageOptions : StorageOptions =>
            application
                .AddModule<ImgProxyStorageModule<TStorageOptions>, ImgProxyStorageModuleOptions<TStorageOptions>>(
                    configure, optionsKey);

        public static Application AddImgProxyStorage<TStorageOptions>(this Application application,
            Action<ImgProxyStorageModuleOptions<TStorageOptions>>? configure = null,
            string? optionsKey = null)
            where TStorageOptions : StorageOptions =>
            application
                .AddModule<ImgProxyStorageModule<TStorageOptions>, ImgProxyStorageModuleOptions<TStorageOptions>>(
                    configure, optionsKey);
    }
}
