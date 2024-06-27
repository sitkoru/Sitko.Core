using JetBrains.Annotations;

namespace Sitko.Core.ServiceDiscovery;

[PublicAPI]
public record ServiceDiscoveryService(string Type, string Name, Dictionary<string, string> Metadata, List<string>? PortNames = null);
