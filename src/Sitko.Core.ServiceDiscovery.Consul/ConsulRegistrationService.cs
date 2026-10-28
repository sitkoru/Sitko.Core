using Consul;
using Microsoft.AspNetCore.Server.Kestrel.Core;
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
        IOptions<ServiceDiscoveryOptions> services,
        IOptions<AppWebConfigurationModuleOptions> web,
        IOptions<ConsulServiceDiscoveryOptions> options,
        IConsulClient consul,
        ILogger<ConsulRegistrationService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (services.Value.Services.Count == 0)
            {
                return;
            }
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = lifetime.ApplicationStarted.Register(() => started.TrySetResult());
            await started.Task.WaitAsync(stoppingToken);
            var registrations = CreateRegistrations();
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
                var ports = web.Value.Ports.Where(port => port.Value.UseTLS &&
                    port.Value.Protocol.HasFlag(HttpProtocols.Http2) &&
                    (service.PortNames is null || service.PortNames.Contains(port.Key, StringComparer.OrdinalIgnoreCase))).ToArray();
                if (ports.Length != 1)
                {
                    throw new InvalidOperationException($"Contract {service.Name} requires one named TLS HTTP/2 port");
                }
                var (name, port) = ports[0];
                var actualPort = listeners[name].IPEndPoint?.Port ?? 0;
                if (actualPort <= 0 || string.IsNullOrWhiteSpace(port.ExternalAddress))
                {
                    throw new InvalidOperationException($"Contract {service.Name} requires a bound port and ExternalAddress");
                }
                return new AgentServiceRegistration
                {
                    ID = $"{context.Id:N}-{ServiceDiscoveryName.Create(context.Environment, service.Name)}",
                    Name = ServiceDiscoveryName.Create(context.Environment, service.Name),
                    Address = port.ExternalAddress,
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
