using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Sitko.Core.Consul.ServiceDiscovery;

namespace Sitko.Core.ServiceDiscovery.Resolver.Consul;

[UsedImplicitly]
public class ConsulServiceDiscoveryResolver(
    IServiceDiscoveryManager serviceDiscoveryManager,
    ILogger<ConsulServiceDiscoveryResolver> logger) : BaseServiceDiscoveryResolver(logger)
{
    protected override async Task<ICollection<ResolvedService>?> DoLoadServicesAsync(CancellationToken cancellationToken) =>
        await serviceDiscoveryManager.LoadAsync(cancellationToken);
}
