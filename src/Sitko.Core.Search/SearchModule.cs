using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scrutor;
using Sitko.Core.App;

namespace Sitko.Core.Search;

public interface ISearchModule
{
}

public abstract class SearchModule<TConfig> : BaseApplicationModule<TConfig>, ISearchModule
    where TConfig : SearchModuleOptions, new()
{
    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        TConfig startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        ConfigureSearch(services);
    }

    protected abstract void ConfigureSearch(IServiceCollection services);

    public override async Task InitAsync(IApplicationContext context, IServiceProvider serviceProvider)
    {
        await base.InitAsync(context, serviceProvider);
        var searchProviders = serviceProvider.GetServices<ISearchProvider>();
        var logger = serviceProvider.GetService<ILogger<SearchModule<TConfig>>>();
        if (searchProviders.Any())
        {
#pragma warning disable 4014
            Task.Run(async () =>
#pragma warning restore 4014
            {
                foreach (var searchProvider in searchProviders)
                {
                    try
                    {
                        await searchProvider.InitAsync();
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Error in search provider {Provider} init: {ErrorText}", searchProvider,
                            e.ToString());
                    }
                }
            });
        }
    }
}

public static class SearchModuleExtensions
{
    public static IServiceCollection RegisterSearchProvider<TSearchProvider, TEntity, TEntityPk>(
        this IServiceCollection serviceCollection)
        where TSearchProvider : class, ISearchProvider<TEntity, TEntityPk>
        where TEntity : class =>
        serviceCollection.Scan(a => a.AddType<TSearchProvider>().AsSelfWithInterfaces());
}

public abstract class SearchModuleOptions : BaseModuleOptions
{
}
