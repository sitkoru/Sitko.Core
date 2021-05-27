using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.ImgProxy
{
    public class
        StorageImgProxyModule<TStorageOptions> : BaseApplicationModule<StorageImgProxyModuleConfig<TStorageOptions>>
        where TStorageOptions : StorageOptions
    {
        public override string GetConfigKey()
        {
            return $"Storage:ImgProxy:{typeof(TStorageOptions).Name}";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            StorageImgProxyModuleConfig<TStorageOptions> startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddSingleton<IImgProxyUrlGenerator<TStorageOptions>, ImgProxyUrlGenerator<TStorageOptions>>();
        }

        public override IEnumerable<Type> GetRequiredModules(ApplicationContext context,
            StorageImgProxyModuleConfig<TStorageOptions> config)
        {
            return new[] {typeof(IStorageModule)};
        }
    }

    // Generic parameter is required for dependency injection
    // ReSharper disable once UnusedTypeParameter
    public class StorageImgProxyModuleConfig<TStorageOptions> : BaseModuleConfig where TStorageOptions : StorageOptions
    {
        public string Host { get; set; } = "";
        public string Key { get; set; } = "";
        public string Salt { get; set; } = "";
        public bool EncodeUrls { get; set; } = false;
    }
}
