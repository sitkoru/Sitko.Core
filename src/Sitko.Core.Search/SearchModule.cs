using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sitko.Core.App;

namespace Sitko.Core.Search
{
    public abstract class SearchModule<TConfig> : BaseApplicationModule<TConfig>
        where TConfig : SearchModuleConfig, new()
    {
        protected SearchModule(TConfig config, Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            ConfigureSearch(services);
        }

        protected abstract void ConfigureSearch(IServiceCollection services);

        public override Task InitAsync(IServiceProvider serviceProvider, IConfiguration configuration,
            IHostEnvironment environment)
        {
            var searchProviders = serviceProvider.GetServices<ISearchProvider>();
            var logger = serviceProvider.GetService<ILogger<SearchModule<TConfig>>>();
            if (searchProviders != null)
            {
                Task.Run(async () =>
                {
                    foreach (var searchProvider in searchProviders)
                    {
                        try
                        {
                            await searchProvider.InitAsync();
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, e.ToString());
                        }
                    }
                });
            }

            return Task.CompletedTask;
        }
    }

    public static class SearchModuleExtensions
    {
        public static IServiceCollection RegisterSearchProvider<TSearchProvider, TEntity>(
            this IServiceCollection serviceCollection) where TSearchProvider : class, ISearchProvider<TEntity>
            where TEntity : class
        {
            serviceCollection.AddScoped<TSearchProvider>();
            serviceCollection.AddScoped<ISearchProvider<TEntity>, TSearchProvider>();
            return serviceCollection.AddScoped<ISearchProvider, TSearchProvider>();
        }
    }

    public abstract class SearchModuleConfig
    {
    }
}
