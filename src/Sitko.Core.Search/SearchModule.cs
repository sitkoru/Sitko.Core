using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scrutor;
using Sitko.Core.App;

namespace Sitko.Core.Search
{
    public interface ISearchModule
    {
    }

    public abstract class SearchModule<TConfig> : BaseApplicationModule<TConfig>, ISearchModule
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
        public static IServiceCollection RegisterSearchProvider<TSearchProvider, TEntity, TEntityPk>(
            this IServiceCollection serviceCollection)
            where TSearchProvider : class, ISearchProvider<TEntity, TEntityPk>
            where TEntity : class
        {
            return serviceCollection.Scan(a => a.AddType<TSearchProvider>().AsSelfWithInterfaces());
        }
    }

    public abstract class SearchModuleConfig
    {
    }
}
