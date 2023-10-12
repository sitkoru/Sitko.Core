using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server.Consul;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddConsulGrpcServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, ConsulDiscoveryGrpcServerModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddConsulGrpcServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddConsulGrpcServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<ConsulDiscoveryGrpcServerModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddConsulGrpcServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddConsulGrpcServer(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ConsulDiscoveryGrpcServerModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<ConsulDiscoveryGrpcServerModule, ConsulDiscoveryGrpcServerModuleOptions>(
            configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddConsulGrpcServer(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<ConsulDiscoveryGrpcServerModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<ConsulDiscoveryGrpcServerModule, ConsulDiscoveryGrpcServerModuleOptions>(
            configure, optionsKey);
}
