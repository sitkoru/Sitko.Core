namespace Sitko.Core.ServiceDiscovery;

public record ResolvedService(
    string Type,
    string Name,
    Dictionary<string, string> Metadata,
    string Scheme,
    string Host,
    int Port);