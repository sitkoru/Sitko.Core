namespace Sitko.Core.ServiceDiscovery;

public interface IServiceDiscoveryRegistrar
{
    Task RegisterAsync(CancellationToken cancellationToken = default);
    Task UnregisterAsync(CancellationToken cancellationToken = default);
    Task RefreshAsync(CancellationToken cancellationToken = default);
}

internal class NopeServiceDiscoveryRegistrar : IServiceDiscoveryRegistrar
{
    public Task RegisterAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task UnregisterAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
