using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App;
using Sitko.Core.App.Helpers;
using Sitko.Core.Grpc.Server.Discovery;
using Tempus;

namespace Sitko.Core.Grpc.Server.Consul
{
    public class ConsulGrpcServicesRegistrar : IGrpcServicesRegistrar, IAsyncDisposable
    {
        private readonly IOptionsMonitor<ConsulDiscoveryGrpcServerModuleOptions> optionsMonitor;
        private readonly IApplication application;
        private readonly IConsulClient? consulClient;
        private readonly string host = "127.0.0.1";
        private readonly bool inContainer = DockerHelper.IsRunningInDocker();
        private readonly ILogger<ConsulGrpcServicesRegistrar> logger;
        private ConsulDiscoveryGrpcServerModuleOptions Options => optionsMonitor.CurrentValue;
        private readonly int port;

        private readonly ConcurrentDictionary<string, string> registeredServices = new();

        private bool disposed;
        private IScheduledTask? updateTtlTask;

        public ConsulGrpcServicesRegistrar(IOptionsMonitor<ConsulDiscoveryGrpcServerModuleOptions> optionsMonitor,
            IApplication application,
            IServer server, IScheduler scheduler, ILogger<ConsulGrpcServicesRegistrar> logger,
            IConsulClient? consulClient = null)
        {
            this.optionsMonitor = optionsMonitor;
            this.application = application;
            this.consulClient = consulClient;
            this.logger = logger;
            if (!string.IsNullOrEmpty(Options.Host))
            {
                this.logger.LogInformation("Use grpc host from config");
                host = Options.Host;
            }
            else if (inContainer)
            {
                this.logger.LogInformation("Use docker ip as grpc host");
                var dockerIp = DockerHelper.GetContainerAddress();
                if (string.IsNullOrEmpty(dockerIp))
                {
                    throw new Exception("Can't find host ip for grpc");
                }

                host = dockerIp;
            }

            this.logger.LogInformation("GRPC Host: {Host}", host);
            if (Options.Port != null && Options.Port > 0)
            {
                this.logger.LogInformation("Use grpc port from config");
                port = Options.Port.Value;
            }
            else
            {
                var serverAddressesFeature = server.Features.Get<IServerAddressesFeature>();
                var address = serverAddressesFeature.Addresses.Select(a => new Uri(a))
                    .FirstOrDefault(u => u.Scheme == "https");
                if (address == null)
                {
                    throw new Exception("Can't find https address for grpc service");
                }

                port = address.Port > 0 ? address.Port : 443;
            }

            this.logger.LogInformation("GRPC Port: {Port}", port);
            //_updateTtlTask = UpdateChecksAsync(_updateTtlCts.Token);
            updateTtlTask = scheduler.Schedule(TimeSpan.FromSeconds(15), async token =>
            {
                await UpdateServicesTtlAsync(token);
            }, (context, _) =>
            {
                this.logger.LogError(context.Exception, "Error updating TTL for gRPC services: {ErrorText}",
                    context.Exception.ToString());
                return Task.CompletedTask;
            });
        }


        public async ValueTask DisposeAsync()
        {
            if (!disposed && consulClient != null)
            {
                if (updateTtlTask != null)
                {
                    await updateTtlTask.Cancel();
                    updateTtlTask = null;
                }

                foreach (var registeredService in registeredServices)
                {
                    logger.LogInformation(
                        "Application stopping. Deregister grpc service {ServiceName} on {Address}:{Port}",
                        registeredService.Value, host,
                        port);
                    await consulClient.Agent.ServiceDeregister(registeredService.Key);
                }

                disposed = true;
            }
        }

        public async Task RegisterAsync<T>() where T : class
        {
            var serviceName = GetServiceName<T>();
            var id = GetServiceId<T>();
            if (consulClient != null)
            {
                var registration = new AgentServiceRegistration
                {
                    ID = id,
                    Name = serviceName,
                    Address = host,
                    Port = port,
                    Check = new AgentServiceCheck
                    {
                        TTL = TimeSpan.FromSeconds(Options.ChecksIntervalInSeconds),
                        DeregisterCriticalServiceAfter =
                            TimeSpan.FromSeconds(Options.DeregisterTimeoutInSeconds)
                    },
                    Tags = new[] {"grpc", $"version:{application.Version}"}
                };
                logger.LogInformation("Register grpc service {ServiceName} on {Address}:{Port}", serviceName, host,
                    port);
                await consulClient.Agent.ServiceDeregister(id);
                var result = await consulClient.Agent.ServiceRegister(registration);
                logger.LogInformation("Consul response code: {Code}", result.StatusCode);
            }

            registeredServices.TryAdd(id, serviceName);
        }

        public async Task<HealthCheckResult> CheckHealthAsync<T>(CancellationToken cancellationToken = default)
            where T : class
        {
            if (consulClient == null)
            {
                return HealthCheckResult.Unhealthy("No consul client");
            }

            var id = GetServiceId<T>();
            var serviceName = GetServiceName<T>();

            var serviceResponse = await consulClient.Catalog.Service(serviceName, "grpc", cancellationToken);
            if (serviceResponse.StatusCode == HttpStatusCode.OK)
            {
                if (serviceResponse.Response.Any())
                {
                    if (serviceResponse.Response.Any(service => service.ServiceID == id))
                    {
                        return HealthCheckResult.Healthy();
                    }

                    return HealthCheckResult.Degraded($"Service {serviceName} exists but with another id");
                }

                if (Options.AutoFixRegistration)
                {
                    //no services. fix registration
                    await RegisterAsync<T>();
                }

                return HealthCheckResult.Degraded($"No grpc service registered with name {serviceName}");
            }

            return HealthCheckResult.Unhealthy($"Error response from consul: {serviceResponse.StatusCode}");
        }

        private string GetServiceName<T>()
        {
            var serviceName = typeof(T).BaseType?.DeclaringType?.Name;
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new Exception($"Can't find service name for {typeof(T)}");
            }

            return serviceName;
        }

        private string GetServiceId<T>()
        {
            var serviceName = GetServiceName<T>();
            return inContainer ? $"{serviceName}_{host}_{port}" : serviceName;
        }

        private async Task UpdateServicesTtlAsync(CancellationToken token)
        {
            if (!token.IsCancellationRequested && consulClient != null && registeredServices.Any())
            {
                logger.LogDebug("Update TTL for gRPC services");
                foreach (var service in registeredServices)
                {
                    logger.LogDebug("Service: {ServiceId}/{ServiceName}", service.Key, service.Value);
                    try
                    {
                        await consulClient.Agent.UpdateTTL("service:" + service.Key,
                            $"Last update: {DateTime.UtcNow:O}", TTLStatus.Pass,
                            token);
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Error updating TTL for {ServiceId}/{ServiceName}: {ErrorText}",
                            service.Key, service.Value, exception.ToString());
                    }
                }

                logger.LogDebug("All gRPC services TTL updated");
            }
        }
    }
}
