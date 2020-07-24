using System;
using System.Collections.Generic;
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

namespace Sitko.Core.Grpc.Server.Consul
{
    public class GrpcServicesRegistrar : IGrpcServicesRegistrar, IAsyncDisposable
    {
        private readonly GrpcServerOptions _options;
        private readonly IApplication _application;
        private readonly IConsulClient? _consulClient;
        private readonly ILogger<GrpcServicesRegistrar> _logger;
        private readonly string _host = "127.0.0.1";
        private readonly int _port;
        private readonly bool _inContainer = DockerHelper.IsRunningInDocker();

        private readonly Dictionary<string, string> _registeredServices = new Dictionary<string, string>();

        public GrpcServicesRegistrar(GrpcServerOptions options,
            IApplication application,
            IServer server, ILogger<GrpcServicesRegistrar> logger, IConsulClient? consulClient = null)
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
                        DeregisterCriticalServiceAfter = _options.DeregisterTimeout,
                        Interval = _options.ChecksInterval,
                        GRPC = $"{_host}:{_port}",
                        TLSSkipVerify = _options.ValidateTls,
                        GRPCUseTLS = _options.UseTls
                    },
                    Tags = new[] {"grpc", $"version:{_application.Version}"}
                };
                _logger.LogInformation("Register grpc service {serviceName} on {address}:{port}", serviceName, _host,
                    _port);
                await _consulClient.Agent.ServiceDeregister(id);
                var result = await _consulClient.Agent.ServiceRegister(registration);
                _logger.LogInformation("Consul response code: {Code}", result.StatusCode);
            }

            _registeredServices[id] = serviceName;
        }

        private bool _disposed;

        public async ValueTask DisposeAsync()
        {
            if (!_disposed && _consulClient != null)
            {
                foreach (var registeredService in _registeredServices)
                {
                    _logger.LogInformation(
                        "Application stopping. Deregister grpc service {serviceName} on {address}:{port}",
                        registeredService.Value, _host,
                        _port);
                    await _consulClient.Agent.ServiceDeregister(registeredService.Key);
                }

                _disposed = true;
            }
        }

        public async Task<HealthCheckResult> CheckHealthAsync<T>(
            CancellationToken cancellationToken = new CancellationToken()) where T : class
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
    }
}
