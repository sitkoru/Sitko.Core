namespace Sitko.Core.ServiceDiscovery;

public interface IServiceDiscoveryResolver
{
    ResolvedService[]? Resolve(string type, string name);
    Task LoadAsync(CancellationToken cancellationToken = default);
    void Subscribe(string serviceType, string name, Action<ResolvedService[]> callback);
}
