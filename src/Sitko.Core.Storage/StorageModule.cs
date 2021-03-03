using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage
{
    public interface IStorageModule : IApplicationModule
    {
    }

    public abstract class StorageModule<TStorage, TStorageOptions> : BaseApplicationModule<TStorageOptions>,
        IStorageModule
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
            services.AddSingleton<TStorage>();
            Config.ConfigureCache?.Invoke(environment, configuration, services);
            Config.ConfigureMetadata?.Invoke(environment, configuration, services);
        }

        public override void CheckConfig()
        {
            base.CheckConfig();
            if (Config.PublicUri is null)
            {
                throw new ArgumentException("Storage url is empty");
            }

            if (string.IsNullOrEmpty(Config.Name))
            {
                throw new ArgumentException("Storage name is empty");
            }
        }

        public override async Task InitAsync(IServiceProvider serviceProvider, IConfiguration configuration,
            IHostEnvironment environment)
        {
            await base.InitAsync(serviceProvider, configuration, environment);
            var metadataProvider = serviceProvider.GetService<IStorageMetadataProvider>();
            if (metadataProvider is not null)
            {
                await metadataProvider.InitAsync();
            }
        }
    }
}
