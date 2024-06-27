using System.Collections.Concurrent;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App.Web;
using Sitko.Core.Consul.ServiceDiscovery;

namespace Sitko.Core.ServiceDiscovery.Server.Consul;

[UsedImplicitly]
public class ConsulServiceDiscoveryRegistrar(
    IServiceDiscoveryManager serviceDiscoveryManager,
    IOptionsMonitor<AppWebConfigurationModuleOptions> hostOptions,
    IOptionsMonitor<ServiceDiscoveryOptions> providerOptions,
    IServer server,
    ILogger<ConsulServiceDiscoveryRegistrar> logger)
    : BaseServiceDiscoveryRegistrar(hostOptions, providerOptions, server, logger)
{
    private readonly ConcurrentBag<(string Id, string ServiceName)> registeredServices = new();

    public override async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Update TTL for SD services");
        foreach (var service in registeredServices)
        {
            logger.LogDebug("Service: {ServiceId}", service);
            await serviceDiscoveryManager.RefreshTtlAsync(service.Id, cancellationToken);
        }

        logger.LogDebug("All SD services TTL updated");
    }

    protected override async Task DoRegisterAsync(
        Dictionary<ApplicationService, List<ServiceDiscoveryService>> registry,
        CancellationToken cancellationToken)
    {
        foreach (var (appService, services) in registry)
        {
            if (services.Count == 0)
            {
                logger.LogInformation("No service for port {PortName}, skipping...", appService.Name);
                continue;
            }

            var serviceName =
                await serviceDiscoveryManager.RegisterAsync(appService, services, cancellationToken);
            Logger.LogInformation("Registered grpc service {ServiceName} on {Address}:{Port}", serviceName,
                appService.Address,
                appService.Port);

            registeredServices.Add(serviceName);
        }
    }

    protected override async Task DoUnregisterAsync(Dictionary<ApplicationService, List<ServiceDiscoveryService>> registry,
        CancellationToken cancellationToken)
    {
        foreach (var registeredService in registeredServices)
        {
            logger.LogInformation("Application stopping. Deregister app service {ServiceName}", registeredService.ServiceName);
            await serviceDiscoveryManager.DeregisterAsync(registeredService.Id, cancellationToken);
        }
    }
}
