using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scrutor;
using Sitko.Core.App;

namespace Sitko.Core.Search;

public interface ISearchModule;

public abstract class SearchModule<TConfig> : BaseApplicationModule<TConfig>, ISearchModule
    where TConfig : SearchModuleOptions, new()
{
    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TConfig startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        ConfigureSearch(services);
    }

    protected abstract void ConfigureSearch(IServiceCollection services);

    public override async Task InitAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider)
    {
        await base.InitAsync(applicationContext, serviceProvider);
        var searchProviders = serviceProvider.GetServices<ISearchProvider>().ToList();
        var logger = serviceProvider.GetRequiredService<ILogger<SearchModule<TConfig>>>();
        if (searchProviders.Count != 0 && GetOptions(serviceProvider).InitProviders)
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
    public static IServiceCollection RegisterSearchProvider<TSearchProvider, TEntity, TEntityPk, TSearchModel>(
        this IServiceCollection serviceCollection)
        where TSearchProvider : class, ISearchProvider<TEntity, TEntityPk, TSearchModel>
        where TEntity : class
        where TSearchModel : BaseSearchModel =>
        serviceCollection.Scan(a => a.FromType<TSearchProvider>().AsSelfWithInterfaces());
}

public abstract class SearchModuleOptions : BaseModuleOptions;
