using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddGrpcServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, GrpcServerModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddGrpcServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddGrpcServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<GrpcServerModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddGrpcServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddGrpcServer(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, GrpcServerModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<GrpcServerModule, GrpcServerModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddGrpcServer(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<GrpcServerModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<GrpcServerModule, GrpcServerModuleOptions>(configure, optionsKey);
}
