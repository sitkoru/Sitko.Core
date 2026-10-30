using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Sitko.Core.ServiceDiscovery.Consul")]

namespace Sitko.Core.ServiceDiscovery;

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
