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
using Sitko.Core.App;
using Sitko.Core.App.Helpers;
using Sitko.Core.Grpc.Server.Discovery;

namespace Sitko.Core.Grpc.Server.Consul
{
    public class ConsulGrpcServicesRegistrar : IGrpcServicesRegistrar, IAsyncDisposable
    {
        private readonly GrpcServerConsulModuleConfig _options;
        private readonly IApplication _application;
        private readonly IConsulClient? _consulClient;
        private readonly ILogger<ConsulGrpcServicesRegistrar> _logger;
        private readonly string _host = "127.0.0.1";
        private readonly int _port;
        private readonly bool _inContainer = DockerHelper.IsRunningInDocker();
        private readonly CancellationTokenSource _updateTtlCts = new();
        private Task? _updateTtlTask;

        private readonly ConcurrentDictionary<string, string> _registeredServices = new();

        public ConsulGrpcServicesRegistrar(GrpcServerConsulModuleConfig options,
            IApplication application,
            IServer server, ILogger<ConsulGrpcServicesRegistrar> logger, IConsulClient? consulClient = null)
        {
            _options = options;
            _application = application;
            _consulClient = consulClient;
            _logger = logger;
            if (!string.IsNullOrEmpty(_options.Host))
            {
                _logger.LogInformation("Use grpc host from config");
                _host = _options.Host;
            }
            else if (_inContainer)
            {
                _logger.LogInformation("Use docker ip as grpc host");
                var dockerIp = DockerHelper.GetContainerAddress();
                if (string.IsNullOrEmpty(dockerIp))
                {
                    throw new Exception("Can't find host ip for grpc");
                }

                _host = dockerIp;
            }

            _logger.LogInformation("GRPC Host: {Host}", _host);
            if (_options.Port != null && _options.Port > 0)
            {
                _logger.LogInformation("Use grpc port from config");
                _port = _options.Port.Value;
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

                _port = address.Port > 0 ? address.Port : 443;
            }

            _logger.LogInformation("GRPC Port: {Port}", _port);
            _updateTtlTask = UpdateChecksAsync(_updateTtlCts.Token);
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
            return _inContainer ? $"{serviceName}_{_host}_{_port}" : serviceName;
        }

        public async Task RegisterAsync<T>() where T : class
        {
            var serviceName = GetServiceName<T>();
            var id = GetServiceId<T>();
            if (_consulClient != null)
            {
                var registration = new AgentServiceRegistration
                {
                    ID = id,
                    Name = serviceName,
                    Address = _host,
                    Port = _port,
                    Check = new AgentServiceCheck
                    {
                        TTL = _options.ChecksInterval,
                        DeregisterCriticalServiceAfter = _options.DeregisterTimeout
                    },
                    Tags = new[] {"grpc", $"version:{_application.Version}"}
                };
                _logger.LogInformation("Register grpc service {ServiceName} on {Address}:{Port}", serviceName, _host,
                    _port);
                await _consulClient.Agent.ServiceDeregister(id);
                var result = await _consulClient.Agent.ServiceRegister(registration);
                _logger.LogInformation("Consul response code: {Code}", result.StatusCode);
            }

            _registeredServices.TryAdd(id, serviceName);
        }

        private bool _disposed;

        public async ValueTask DisposeAsync()
        {
            if (!_disposed && _consulClient != null)
            {
                if (_updateTtlTask != null)
                {
                    _updateTtlCts.Cancel();
                    await _updateTtlTask;
                    _updateTtlTask = null;
                }

                foreach (var registeredService in _registeredServices)
                {
                    _logger.LogInformation(
                        "Application stopping. Deregister grpc service {ServiceName} on {Address}:{Port}",
                        registeredService.Value, _host,
                        _port);
                    await _consulClient.Agent.ServiceDeregister(registeredService.Key);
                }

                _disposed = true;
            }
        }

        public async Task<HealthCheckResult> CheckHealthAsync<T>(CancellationToken cancellationToken = default)
            where T : class
        {
            if (_consulClient == null)
            {
                return HealthCheckResult.Unhealthy("No consul client");
            }

            var id = GetServiceId<T>();
            var serviceName = GetServiceName<T>();

            var serviceResponse = await _consulClient.Catalog.Service(serviceName, "grpc", cancellationToken);
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

                if (_options.AutoFixRegistration)
                {
                    //no services. fix registration
                    await RegisterAsync<T>();
                }

                return HealthCheckResult.Degraded($"No grpc service registered with name {serviceName}");
            }

            return HealthCheckResult.Unhealthy($"Error response from consul: {serviceResponse.StatusCode}");
        }

        private async Task UpdateChecksAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
                if (!cancellationToken.IsCancellationRequested && _consulClient != null && _registeredServices.Any())
                {
                    foreach (var service in _registeredServices)
                    {
                        await _consulClient.Agent.UpdateTTL("service:" + service.Key,
                            $"Last update: {DateTime.UtcNow:O}", TTLStatus.Pass,
                            cancellationToken);
                    }
                }
            }
        }
    }
}
