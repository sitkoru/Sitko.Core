using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sitko.Core.ServiceDiscovery;

public class ServiceDiscoveryRefresherService(
    IServiceDiscoveryRegistrar registrar,
    IOptionsMonitor<ServiceDiscoveryModuleOptions> hostOptions,
    ILogger<ServiceDiscoveryRefresherService> logger)
    : BackgroundService
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
