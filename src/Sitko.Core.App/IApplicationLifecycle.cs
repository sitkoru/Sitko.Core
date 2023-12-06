namespace Sitko.Core.App;

public interface IApplicationLifecycle
{
    Task StartingAsync(CancellationToken cancellationToken);

    Task StartedAsync(CancellationToken cancellationToken);

    Task StoppingAsync(CancellationToken cancellationToken);

    Task StoppedAsync(CancellationToken cancellationToken);
}
