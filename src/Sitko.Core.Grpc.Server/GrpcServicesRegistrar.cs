using System;
using System.Linq;
using System.Threading.Tasks;
using Consul;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Helpers;

namespace Sitko.Core.Grpc.Server
{
    public class GrpcServicesRegistrar
    {
        private readonly GrpcServerOptions _options;
        private readonly IConsulClient _consulClient;
        private readonly ILogger<GrpcServicesRegistrar> _logger;
        private readonly string _host;
        private readonly int _port;
        private readonly bool _inContainer;

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
                    throw new Exception($"Can't find host ip for grpc");
                }
            }

            IServerAddressesFeature serverAddressesFeature = server.Features.Get<IServerAddressesFeature>();
            var address = new Uri(serverAddressesFeature.Addresses.First());
            _port = address.Port;
        }

        public async Task RegisterAsync<T>() where T : class
        {
            var serviceName = typeof(T).BaseType?.DeclaringType.Name;
            var id = _inContainer ? $"{serviceName}_{_host}_{_port}" : serviceName;
            var registration = new AgentServiceRegistration
            {
                ID = id,
                Name = serviceName,
                Address = _host,
                Port = _port,
                Check = new AgentServiceCheck
                {
                    DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1),
                    Interval = TimeSpan.FromSeconds(30),
                    GRPC = $"{_host}:{_port}"
                },
                Tags = new[] {"grpc", $"version:{_options.Version}"}
            };
            _logger.LogInformation("Register grpc service {serviceName} on {address}:{port}", serviceName, _host,
                _port);
            await _consulClient.Agent.ServiceDeregister(id);
            await _consulClient.Agent.ServiceRegister(registration);
        }

        public async Task StopAsync<T>()
        {
            var serviceName = typeof(T).BaseType?.DeclaringType.Name;
            var id = _inContainer ? $"{serviceName}_{_host}_{_port}" : serviceName;
            await _consulClient.Agent.ServiceDeregister(id);
        }
    }
}
