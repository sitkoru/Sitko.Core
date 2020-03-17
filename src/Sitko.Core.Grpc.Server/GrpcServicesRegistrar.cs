using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Consul;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Helpers;

namespace Sitko.Core.Grpc.Server
{
    public class GrpcServicesRegistrar : IAsyncDisposable
    {
        private readonly GrpcServerOptions _options;
        private readonly IConsulClient _consulClient;
        private readonly ILogger<GrpcServicesRegistrar> _logger;
        private readonly string? _host;
        private readonly int _port;
        private readonly bool _inContainer;

        private readonly Dictionary<string, string> _registeredServices = new Dictionary<string, string>();

        public GrpcServicesRegistrar(GrpcServerOptions options, IConsulClient consulClient,
            IServer server, ILogger<GrpcServicesRegistrar> logger)
        {
            _options = options;
            _consulClient = consulClient;
            _logger = logger;

            _host = options.IpAddress ?? "127.0.0.1"; // windows machine
            _inContainer = DockerHelper.IsRunningInDocker();
            if (_inContainer)
            {
                _host = DockerHelper.GetContainerAddress();
                if (string.IsNullOrEmpty(_host))
                {
                    throw new Exception("Can't find host ip for grpc");
                }
            }

            IServerAddressesFeature serverAddressesFeature = server.Features.Get<IServerAddressesFeature>();
            var address = new Uri(serverAddressesFeature.Addresses.First());
            _port = address.Port;
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
                    GRPC = $"{_host}:{_port}"
                },
                Tags = new[] {"grpc", $"version:{_options.Version}"}
            };
            _logger.LogInformation("Register grpc service {serviceName} on {address}:{port}", serviceName, _host,
                _port);
            await _consulClient.Agent.ServiceDeregister(id);
            await _consulClient.Agent.ServiceRegister(registration);
            _registeredServices.Add(id, serviceName);
        }

        private bool _disposed;

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
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

        public async Task<bool> IsRegistered<T>() where T : class
        {
            var id = GetServiceId<T>();
            var serviceName = GetServiceName<T>();
            var serviceResponse = await _consulClient.Catalog.Service(serviceName, "grpc");
            if (serviceResponse.StatusCode == HttpStatusCode.OK)
            {
                if (serviceResponse.Response.Any())
                {
                    return serviceResponse.Response.Any(service => service.ServiceID == id);
                }
            }

            return false;
        }
    }
}
