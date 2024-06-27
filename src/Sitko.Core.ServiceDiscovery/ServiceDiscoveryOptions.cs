using System.Runtime.CompilerServices;
using JetBrains.Annotations;

[assembly: InternalsVisibleTo("Sitko.Core.ServiceDiscovery.Server")]

namespace Sitko.Core.ServiceDiscovery;

[PublicAPI]
public class ServiceDiscoveryOptions
{
    private readonly HashSet<ServiceDiscoveryService> services = new();

    internal ICollection<ServiceDiscoveryService> Services => services.ToArray();

    public ServiceDiscoveryOptions RegisterService(ServiceDiscoveryService service)
    {
        services.Add(service);
        return this;
    }

    public ServiceDiscoveryOptions Clear()
    {
        services.Clear();
        return this;
    }
}
