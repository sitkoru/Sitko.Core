using System.Net;
using Consul;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.ServiceDiscovery.Consul
{
    internal sealed class ConsulRegistrationService(
        IApplicationContext context,
        IHostApplicationLifetime lifetime,
        IServiceProvider serviceProvider,
        IOptions<ServiceDiscoveryOptions> services,
        IOptions<AppWebConfigurationModuleOptions> web,
        IOptions<ConsulServiceDiscoveryOptions> options,
        IConsulClient consul,
        ILogger<ConsulRegistrationService> logger) : BackgroundService, IHostedLifecycleService
    {
        private AgentServiceRegistration[] registrations = [];

        public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StartedAsync(CancellationToken cancellationToken)
        {
            if (services.Value.Services.Count > 0)
            {
                registrations = CreateRegistrations();
            }
            return Task.CompletedTask;
        }

        public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (services.Value.Services.Count == 0)
            {
                return;
            }
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = lifetime.ApplicationStarted.Register(() => started.TrySetResult());
            await started.Task.WaitAsync(stoppingToken);
            var registered = false;
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        foreach (var service in registrations)
                        {
                            if (!registered)
                            {
                                _ = await consul.Agent.ServiceRegister(service, stoppingToken);
                            }
                            await consul.Agent.PassTTL($"service:{service.ID}", "", stoppingToken);
                        }
                        registered = true;
                    }
                    catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                    {
                        registered = false;
                        logger.LogWarning(exception, "Cannot refresh Consul contract registrations");
                    }
                    await Task.Delay(registered ? options.Value.HeartbeatInterval : options.Value.RegistrationRetryInterval, stoppingToken);
                }
            }
            finally
            {
                using var cleanup = new CancellationTokenSource(options.Value.RequestTimeout);
                foreach (var service in registrations)
                {
                    try
                    {
                        _ = await consul.Agent.ServiceDeregister(service.ID, cleanup.Token);
                    }
                    catch (Exception exception)
                    {
                        logger.LogWarning(exception, "Cannot remove own Consul registration {ServiceId}", service.ID);
                    }
                }
            }
        }

        private AgentServiceRegistration[] CreateRegistrations()
        {
            var listeners = context.GetModuleInstance<AppWebConfigurationModule>().Listeners;
            return [.. services.Value.Services.Select(service =>
            {
                string address;
                int actualPort;
                if (web.Value.Ports.Count == 0)
                {
                    var addresses = serviceProvider.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses
                        .Select(value => Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null)
                        .Where(uri => uri is { Scheme: "https", Port: > 0 } &&
                            (service.PortNames is null || service.PortNames.Contains("https", StringComparer.OrdinalIgnoreCase)))
                        .ToArray() ?? [];
                    if (addresses.Length != 1)
                    {
                        throw new InvalidOperationException($"Contract {service.Name} requires one bound HTTPS address");
                    }
                    var uri = addresses[0]!;
                    address = uri.DnsSafeHost switch
                    {
                        "*" or "+" or "0.0.0.0" => "127.0.0.1",
                        "::" => "::1",
                        _ => uri.DnsSafeHost
                    };
                    actualPort = uri.Port;
                }
                else
                {
                    var ports = web.Value.Ports.Where(port => port.Value.UseTLS &&
                        port.Value.Protocol.HasFlag(HttpProtocols.Http2) &&
                        (service.PortNames is null || service.PortNames.Contains(port.Key, StringComparer.OrdinalIgnoreCase))).ToArray();
                    if (ports.Length != 1)
                    {
                        throw new InvalidOperationException($"Contract {service.Name} requires one named TLS HTTP/2 port");
                    }
                    var (name, port) = ports[0];
                    if (!listeners.TryGetValue(name, out var listener) ||
                        !listener.Protocols.HasFlag(HttpProtocols.Http2))
                    {
                        throw new InvalidOperationException($"Contract {service.Name} requires its named TLS HTTP/2 listener {name}");
                    }
                    actualPort = listener.IPEndPoint?.Port ?? 0;
                    var wildcard = port.ExternalAddress is "*" or "+" ||
                        (IPAddress.TryParse(port.ExternalAddress, out var parsedAddress) &&
                        (parsedAddress.Equals(IPAddress.Any) || parsedAddress.Equals(IPAddress.IPv6Any) ||
                         (parsedAddress.IsIPv4MappedToIPv6 && parsedAddress.MapToIPv4().Equals(IPAddress.Any))));
                    if (actualPort <= 0 || string.IsNullOrWhiteSpace(port.ExternalAddress) || wildcard)
                    {
                        throw new InvalidOperationException($"Contract {service.Name} requires a bound positive port and non-wildcard ExternalAddress");
                    }
                    address = port.ExternalAddress;
                }
                return new AgentServiceRegistration
                {
                    ID = $"{context.Id:N}-{ServiceDiscoveryName.Create(context.Environment, service.Name)}",
                    Name = ServiceDiscoveryName.Create(context.Environment, service.Name),
                    Address = address,
                    Port = actualPort,
                    Meta = new Dictionary<string, string>
                    {
                        ["contract"] = service.Name,
                        ["environment"] = context.Environment,
                        ["application"] = context.Name
                    },
                    Check = new AgentServiceCheck
                    {
                        TTL = options.Value.Ttl,
                        DeregisterCriticalServiceAfter = options.Value.DeregisterCriticalServiceAfter
                    }
                };
            })];
        }
    }
}
