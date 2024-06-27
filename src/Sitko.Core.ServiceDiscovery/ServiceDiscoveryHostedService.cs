using Microsoft.Extensions.Hosting;

namespace Sitko.Core.ServiceDiscovery;

public class ServiceDiscoveryHostedService(IServiceDiscoveryRegistrar registrar) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => registrar.RegisterAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => registrar.UnregisterAsync(cancellationToken);
}
