using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery;

namespace Sitko.Core.ServiceDiscovery;

public interface IServiceDiscoveryModule;

public class ServiceDiscoveryModule : BaseApplicationModule<ServiceDiscoveryModuleOptions>, IServiceDiscoveryModule
{
    public override string OptionsKey => "ServiceDiscovery";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        ServiceDiscoveryModuleOptions startupOptions) => services.AddServiceDiscoveryCore();

    public override Task InitAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var declarations = serviceProvider.GetRequiredService<IOptions<ServiceDiscoveryOptions>>().Value.Services;
        if (declarations.Count > 0 && !serviceProvider.GetServices<IServiceEndpointProviderFactory>().Any())
        {
            throw new InvalidOperationException($"Contracts {string.Join(", ", declarations.Select(static service => service.Name))} require a ServiceDiscovery provider for publication");
        }
        return Task.CompletedTask;
    }
}

public class ServiceDiscoveryModule<TRegistrar, TResolver> : BaseApplicationModule<ServiceDiscoveryModuleOptions>,
    IServiceDiscoveryModule
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

    public override async Task InitAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await base.InitAsync(applicationContext, serviceProvider, cancellationToken);
        var resolver = serviceProvider.GetRequiredService<IServiceDiscoveryResolver>();
        await resolver.LoadAsync(cancellationToken);
    }
}

public class ServiceDiscoveryModuleOptions : BaseModuleOptions
{
    public int RefreshIntervalInSeconds { get; set; } = 15;
}
