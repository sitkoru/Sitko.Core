using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.ServiceDiscovery;

public abstract class BaseServiceDiscoveryResolverModule<TOptions, TResolver> : BaseApplicationModule<TOptions>
    where TOptions : BaseModuleOptions, new() where TResolver : class, IServiceDiscoveryResolver
{
    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<IServiceDiscoveryResolver, TResolver>();
        services.Configure<ServiceDiscoveryOptions>(_ => { });
    }
}
