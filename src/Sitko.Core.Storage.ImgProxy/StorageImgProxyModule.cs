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
        public StorageImgProxyModule(StorageImgProxyModuleConfig<TStorageOptions> config, Application application) :
            base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IImgProxyUrlGenerator<TStorageOptions>, ImgProxyUrlGenerator<TStorageOptions>>();
        }

        public override List<Type> GetRequiredModules()
        {
            return new() { typeof(IStorageModule) };
        }
    }

    // Generic parameter is required for dependency injection
    // ReSharper disable once UnusedTypeParameter
    public class StorageImgProxyModuleConfig<TStorageOptions> where TStorageOptions : StorageOptions
    {
        public string Host { get; set; } = "";
        public string Key { get; set; } = "";
        public string Salt { get; set; } = "";
        public bool EncodeUrls { get; set; } = false;
    }
}
