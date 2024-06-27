namespace Sitko.Core.ServiceDiscovery.Server;

public interface IServiceDiscoveryRegistrar
{
    Task RegisterAsync(CancellationToken cancellationToken = default);
    Task UnregisterAsync(CancellationToken cancellationToken = default);
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
