using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App.Web;

namespace Sitko.Core.ServiceDiscovery.Server;

public abstract class BaseServiceDiscoveryRegistrar(
    IOptionsMonitor<AppWebConfigurationModuleOptions> hostOptions,
    IOptionsMonitor<ServiceDiscoveryOptions> providerOptions,
    IServer server,
    ILogger<BaseServiceDiscoveryRegistrar> logger)
    : IServiceDiscoveryRegistrar
{
    private const string IpV6Localhost = "[::]";
    private const string IpV4Localhost = "127.0.0.1";

    protected ILogger<BaseServiceDiscoveryRegistrar> Logger { get; } = logger;

    public async Task RegisterAsync(CancellationToken cancellationToken = default) =>
        await DoRegisterAsync(BuildRegistry(), cancellationToken);

    public async Task UnregisterAsync(CancellationToken cancellationToken = default) =>
        await DoUnregisterAsync(BuildRegistry(), cancellationToken);


    public abstract Task RefreshAsync(CancellationToken cancellationToken = default);

    private Dictionary<ApplicationService, List<ServiceDiscoveryService>> BuildRegistry()
    {
        var registry = new Dictionary<ApplicationService, List<ServiceDiscoveryService>>();
        if (hostOptions.CurrentValue.Ports.Count != 0)
        {
            foreach (var (portName, applicationPort) in hostOptions.CurrentValue.Ports)
            {
                var address = IpV4Localhost;
                var port = applicationPort.Port;
                if (!string.IsNullOrEmpty(applicationPort.ExternalAddress))
                {
                    address = applicationPort.ExternalAddress;
                    if (applicationPort.ExternalPort > 0)
                    {
                        port = applicationPort.ExternalPort.Value;
                    }
                }
                else
                {
                    var serverAddressesFeature = server.Features.Get<IServerAddressesFeature>();
                    if (serverAddressesFeature is not null)
                    {
                        var addressForPort = serverAddressesFeature.Addresses.Select(a => new Uri(a))
                            .FirstOrDefault(u => u.Port == port);
                        if (addressForPort != null)
                        {
                            if (addressForPort.Host != IpV6Localhost)
                            {
                                address = addressForPort.Host;
                            }
                        }
                    }
                }

                var appService =
                    new ApplicationService(portName, address, port, applicationPort.UseTLS ? "https" : "http");
                registry[appService] = new List<ServiceDiscoveryService>();
            }
        }
        else
        {
            var serverAddressesFeature = server.Features.Get<IServerAddressesFeature>();
            if (serverAddressesFeature is null)
            {
                throw new InvalidOperationException("IServerAddressesFeature not present");
            }

            foreach (var address in serverAddressesFeature.Addresses)
            {
                var uri = new Uri(address);
                var appService =
                    new ApplicationService(uri.Scheme, uri.Host == IpV6Localhost ? IpV4Localhost : uri.Host, uri.Port,
                        uri.Scheme);
                registry[appService] = new List<ServiceDiscoveryService>();
            }
        }

        foreach (var service in providerOptions.CurrentValue.Services)
        {
            var found = false;
            foreach (var (appService, servicesList) in registry)
            {
                if (service.PortNames?.Count > 0 &&
                    !service.PortNames.Any(s => appService.Name.Equals(s, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                servicesList.Add(service);
                found = true;
            }

            if (!found)
            {
                Logger.LogWarning("Can't find host for service {Service}", service);
            }
        }

        return registry;
    }

    protected abstract Task DoRegisterAsync(Dictionary<ApplicationService, List<ServiceDiscoveryService>> registry,
        CancellationToken cancellationToken);

    protected abstract Task DoUnregisterAsync(Dictionary<ApplicationService, List<ServiceDiscoveryService>> registry,
        CancellationToken cancellationToken);
}
