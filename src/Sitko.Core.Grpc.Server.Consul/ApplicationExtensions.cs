using System;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server.Consul;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddConsulGrpcServer(this Application application,
        Action<IConfiguration, IAppEnvironment, ConsulDiscoveryGrpcServerModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<ConsulDiscoveryGrpcServerModule, ConsulDiscoveryGrpcServerModuleOptions>(
            configure, optionsKey);

    public static Application AddConsulGrpcServer(this Application application,
        Action<ConsulDiscoveryGrpcServerModuleOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<ConsulDiscoveryGrpcServerModule, ConsulDiscoveryGrpcServerModuleOptions>(
            configure, optionsKey);
}
