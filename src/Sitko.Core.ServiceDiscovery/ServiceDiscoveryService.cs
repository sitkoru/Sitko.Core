namespace Sitko.Core.ServiceDiscovery;

public record ServiceDiscoveryService(string Type, string Name, Dictionary<string, string> Metadata, List<string>? PortNames = null);
