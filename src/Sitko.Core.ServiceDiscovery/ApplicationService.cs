namespace Sitko.Core.ServiceDiscovery;

public record ApplicationService(
    string Name,
    string Address,
    int Port,
    string Scheme);
