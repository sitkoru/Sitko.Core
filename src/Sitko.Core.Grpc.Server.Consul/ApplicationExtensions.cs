namespace Sitko.Core.Grpc.Server.Consul
{
    using System;
    using App;
    using JetBrains.Annotations;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;

    [PublicAPI]
    public static class ApplicationExtensions
    {
        public static Application AddConsulGrpcServer(this Application application,
            Action<IConfiguration, IHostEnvironment, ConsulDiscoveryGrpcServerModuleOptions> configure,
            string? optionsKey = null) =>
            application.AddModule<ConsulDiscoveryGrpcServerModule, ConsulDiscoveryGrpcServerModuleOptions>(
                configure, optionsKey);

        public static Application AddConsulGrpcServer(this Application application,
            Action<ConsulDiscoveryGrpcServerModuleOptions>? configure = null,
            string? optionsKey = null) =>
            application.AddModule<ConsulDiscoveryGrpcServerModule, ConsulDiscoveryGrpcServerModuleOptions>(
                configure, optionsKey);
    }
}
