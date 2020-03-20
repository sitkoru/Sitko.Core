using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage
{
    public interface IStorageModule : IApplicationModule
    {
    }

    public abstract class StorageModule<TStorage, TStorageOptions> : BaseApplicationModule<TStorageOptions>, IStorageModule
        where TStorage : Storage<TStorageOptions> where TStorageOptions : StorageOptions, new()
    {
        protected StorageModule(TStorageOptions config, Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IStorage<TStorageOptions>, TStorage>();
            Config.ConfigureCache?.Invoke(environment, configuration, services);
        }

        public override void CheckConfig()
        {
            base.CheckConfig();
            if (Config.PublicUri is null)
            {
                throw new ArgumentException("Storage url is empty");
            }
        }
    }
}
