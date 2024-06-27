using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sitko.Core.ServiceDiscovery.Server;

public class ServiceDiscoveryRefresherService<TOptions>(
    IServiceDiscoveryRegistrar registrar,
    IOptionsMonitor<TOptions> hostOptions,
    ILogger<ServiceDiscoveryRefresherService<TOptions>> logger)
    : BackgroundService where TOptions : ServiceDiscoveryModuleOptions, new()
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(hostOptions.CurrentValue.RefreshIntervalInSeconds),
                    stoppingToken);
                await registrar.RefreshAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                //do nothing
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing SD services: {ErrorText}", ex.Message);
            }
        }
    }
}
