using Grpc.Core;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Client.Consul;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddConsulGrpcClient<TClient>(this IHostApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ConsulGrpcClientModuleOptions<TClient>> configure,
        string? optionsKey = null)
        where TClient : ClientBase<TClient>
    {
        applicationBuilder.GetSitkoCore().AddConsulGrpcClient(configure, optionsKey);
        return applicationBuilder;
    }

    public static IHostApplicationBuilder AddConsulGrpcClient<TClient>(this IHostApplicationBuilder applicationBuilder,
        Action<ConsulGrpcClientModuleOptions<TClient>>? configure = null,
        string? optionsKey = null)
        where TClient : ClientBase<TClient>
    {
        applicationBuilder.GetSitkoCore().AddConsulGrpcClient(configure, optionsKey);
        return applicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddConsulGrpcClient<TClient>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ConsulGrpcClientModuleOptions<TClient>> configure,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        applicationBuilder.AddModule<ConsulGrpcClientModule<TClient>, ConsulGrpcClientModuleOptions<TClient>>(configure,
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddConsulGrpcClient<TClient>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<ConsulGrpcClientModuleOptions<TClient>>? configure = null,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        applicationBuilder.AddModule<ConsulGrpcClientModule<TClient>, ConsulGrpcClientModuleOptions<TClient>>(configure,
            optionsKey);
}
