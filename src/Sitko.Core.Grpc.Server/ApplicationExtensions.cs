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
        if (!applicationBuilder.HasModule<GrpcServerModule>())
        {
            List<Action<IApplicationContext, GrpcServerModuleOptions>> configureActions = [configure];
            _ = applicationBuilder.AddModule<GrpcServerModule, GrpcServerModuleOptions>(
                (context, options) => GrpcServerModule.ConfigureOptions(context, options, configureActions), optionsKey);
            applicationBuilder.Context.GetModuleInstance<GrpcServerModule>().ConfigureActions = configureActions;
        }
        else
        {
            applicationBuilder.Context.GetModuleInstance<GrpcServerModule>().ConfigureActions.Add(configure);
        }
        return new GrpcServerBuilder(applicationBuilder.Context.GetModuleInstance<GrpcServerModule>());
    }

    public static GrpcServerBuilder AddGrpcServer(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<GrpcServerModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddGrpcServer((_, options) => configure?.Invoke(options), optionsKey);
}
