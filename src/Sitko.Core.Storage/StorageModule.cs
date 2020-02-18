using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage
{
    public class StorageModule<TStorage, TStorageOptions> : BaseApplicationModule<TStorageOptions>
        where TStorage : Storage<TStorageOptions> where TStorageOptions : StorageOptions
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IStorage<TStorageOptions>, TStorage>();
            Config.ConfigureCache?.Invoke(environment, configuration, services);
        }

        protected override void CheckConfig()
        {
            base.CheckConfig();
            if (Config.PublicUri is null)
            {
                throw new ArgumentException("Storage url is empty");
            }
        }
    }
}
