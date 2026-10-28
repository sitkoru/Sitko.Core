using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server;

public static class ApplicationExtensions
{
    public static GrpcServerBuilder AddGrpcServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, GrpcServerModuleOptions> configure,
        string? optionsKey = null)
        => hostApplicationBuilder.GetSitkoCore().AddGrpcServer(configure, optionsKey);

    public static GrpcServerBuilder AddGrpcServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<GrpcServerModuleOptions>? configure = null,
        string? optionsKey = null)
        => hostApplicationBuilder.GetSitkoCore().AddGrpcServer(configure, optionsKey);

    public static GrpcServerBuilder AddGrpcServer(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, GrpcServerModuleOptions> configure,
        string? optionsKey = null)
    {
        _ = applicationBuilder.AddModule<GrpcServerModule, GrpcServerModuleOptions>(configure, optionsKey);
        return new GrpcServerBuilder(applicationBuilder.Context.GetModuleInstance<GrpcServerModule>());
    }

    public static GrpcServerBuilder AddGrpcServer(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<GrpcServerModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        _ = applicationBuilder.AddModule<GrpcServerModule, GrpcServerModuleOptions>(configure, optionsKey);
        return new GrpcServerBuilder(applicationBuilder.Context.GetModuleInstance<GrpcServerModule>());
    }
}
