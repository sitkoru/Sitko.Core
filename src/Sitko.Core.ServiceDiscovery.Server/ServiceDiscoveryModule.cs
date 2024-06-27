using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.ServiceDiscovery.Server;

public abstract class ServiceDiscoveryServerModule<TOptions, TProvider> : BaseApplicationModule<TOptions>
    where TOptions : ServiceDiscoveryModuleOptions, new() where TProvider : class, IServiceDiscoveryRegistrar
{
    public override string OptionsKey => "ServiceDiscovery";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TOptions startupOptions)
    {
        services.AddSingleton<IServiceDiscoveryRegistrar, TProvider>();
        services.AddHostedService<ServiceDiscoveryHostedService>();
        services.AddHostedService<ServiceDiscoveryRefresherService<TOptions>>();
    }
}
