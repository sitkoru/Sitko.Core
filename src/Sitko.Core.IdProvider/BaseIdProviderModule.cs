using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.IdProvider;

public abstract class BaseIdProviderModule<TProvider, TModuleOptions> : BaseApplicationModule<TModuleOptions>
    where TProvider : class, IIdProvider
    where TModuleOptions : BaseIdProviderModuleOptions<TProvider>, new()
{
    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<IIdProvider, TProvider>();
    }
}

// ReSharper disable once UnusedTypeParameter
public abstract class BaseIdProviderModuleOptions<TProvider> : BaseModuleOptions
    where TProvider : class, IIdProvider;

