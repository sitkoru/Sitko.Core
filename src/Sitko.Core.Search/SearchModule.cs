using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sitko.Core.App;
using Sitko.Core.Repository;

namespace Sitko.Core.Search
{
    public abstract class SearchModule<TAssembly, TConfig> : BaseApplicationModule<TConfig>
        where TConfig : SearchModuleConfig
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddScoped<IRepositoryFilter, SearchRepositoryFilter>();
            ConfigureSearch(services);
        }

        protected abstract void ConfigureSearch(IServiceCollection services);

        public override Task InitAsync(IServiceProvider serviceProvider, IConfiguration configuration,
            IHostEnvironment environment)
        {
            var searchProviders = serviceProvider.GetServices<ISearchProvider>();
            var logger = serviceProvider.GetService<ILogger<SearchModule<TAssembly, TConfig>>>();
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

        public override List<Type> GetRequiredModules()
        {
            return new List<Type> {typeof(RepositoriesModule<TAssembly>)};
        }
    }

    public static class SearchModuleExtensions
    {
        public static IServiceCollection RegisterSearchProvider<TSearchProvider, TEntity>(
            this IServiceCollection serviceCollection) where TSearchProvider : class, ISearchProvider<TEntity>
            where TEntity : IEntity
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
