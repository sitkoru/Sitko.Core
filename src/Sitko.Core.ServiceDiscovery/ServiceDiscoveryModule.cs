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

public class ServiceDiscoveryModuleOptions : BaseModuleOptions
{
}
