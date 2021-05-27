using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.ImgProxy
{
    public class
        StorageImgProxyModule<TStorageOptions> : BaseApplicationModule<StorageImgProxyModuleOptions<TStorageOptions>>
        where TStorageOptions : StorageOptions
    {
        public override string GetOptionsKey()
        {
            return $"Storage:ImgProxy:{typeof(TStorageOptions).Name}";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            StorageImgProxyModuleOptions<TStorageOptions> startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddSingleton<IImgProxyUrlGenerator<TStorageOptions>, ImgProxyUrlGenerator<TStorageOptions>>();
        }

        public override IEnumerable<Type> GetRequiredModules(ApplicationContext context,
            StorageImgProxyModuleOptions<TStorageOptions> options)
        {
            return new[] {typeof(IStorageModule)};
        }
    }

    // Generic parameter is required for dependency injection
    // ReSharper disable once UnusedTypeParameter
    public class StorageImgProxyModuleOptions<TStorageOptions> : BaseModuleOptions where TStorageOptions : StorageOptions
    {
        public string Host { get; set; } = "";
        public string Key { get; set; } = "";
        public string Salt { get; set; } = "";
        public bool EncodeUrls { get; set; } = false;
    }
}
