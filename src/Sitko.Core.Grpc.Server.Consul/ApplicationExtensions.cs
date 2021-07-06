using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server.Consul
{
    public static class ApplicationExtensions
    {
        public static Application AddConsulGrpcServer(this Application application,
            Action<IConfiguration, IHostEnvironment, ConsulDiscoveryGrpcServerModuleOptions> configure,
            string? optionsKey = null)
        {
            return application.AddModule<ConsulDiscoveryGrpcServerModule, ConsulDiscoveryGrpcServerModuleOptions>(
                configure, optionsKey);
        }

        public static Application AddConsulGrpcServer(this Application application,
            Action<ConsulDiscoveryGrpcServerModuleOptions>? configure = null,
            string? optionsKey = null)
        {
            return application.AddModule<ConsulDiscoveryGrpcServerModule, ConsulDiscoveryGrpcServerModuleOptions>(
                configure, optionsKey);
        }
    }
}
