using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App;

internal class HostedLifecycleService(IApplicationLifecycle lifecycle) : IHostedLifecycleService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartingAsync(CancellationToken cancellationToken) =>
        lifecycle.StartingAsync(cancellationToken);

    public Task StartedAsync(CancellationToken cancellationToken) =>
        lifecycle.StartedAsync(cancellationToken);

    public Task StoppingAsync(CancellationToken cancellationToken) =>
        lifecycle.StoppingAsync(cancellationToken);

    public Task StoppedAsync(CancellationToken cancellationToken) =>
        lifecycle.StoppedAsync(cancellationToken);
}
