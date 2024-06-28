using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.ServiceDiscovery;

public class ServiceDiscoveryModule<TRegistrar, TResolver> : BaseApplicationModule<ServiceDiscoveryModuleOptions>
    where TRegistrar : class, IServiceDiscoveryRegistrar
    where TResolver : class, IServiceDiscoveryResolver
{
    public override string OptionsKey => "ServiceDiscovery";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        ServiceDiscoveryModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<IServiceDiscoveryResolver, TResolver>();
        services.Configure<ServiceDiscoveryOptions>(_ => { });

        if (typeof(TRegistrar) != typeof(NopeServiceDiscoveryRegistrar))
        {
            services.AddSingleton<IServiceDiscoveryRegistrar, TRegistrar>();
            services.AddHostedService<ServiceDiscoveryHostedService>();
            services.AddHostedService<ServiceDiscoveryRefresherService>();
        }
    }
}

public class ServiceDiscoveryModuleOptions : BaseModuleOptions
{
    public int RefreshIntervalInSeconds { get; set; } = 15;
}
