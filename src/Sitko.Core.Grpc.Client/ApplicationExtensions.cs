using Grpc.Core;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Grpc.Client.External;

namespace Sitko.Core.Grpc.Client;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddExternalGrpcClient<TClient>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, ExternalGrpcClientModuleOptions<TClient>> configure,
        string? optionsKey = null)
        where TClient : ClientBase<TClient>
    {
        hostApplicationBuilder.AddSitkoCore().AddExternalGrpcClient(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddExternalGrpcClient<TClient>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<ExternalGrpcClientModuleOptions<TClient>>? configure = null,
        string? optionsKey = null)
        where TClient : ClientBase<TClient>
    {
        hostApplicationBuilder.AddSitkoCore().AddExternalGrpcClient(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddExternalGrpcClient<TClient>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ExternalGrpcClientModuleOptions<TClient>> configure,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        applicationBuilder.AddModule<ExternalGrpcClientModule<TClient>, ExternalGrpcClientModuleOptions<TClient>>(
            configure,
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddExternalGrpcClient<TClient>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<ExternalGrpcClientModuleOptions<TClient>>? configure = null,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        applicationBuilder.AddModule<ExternalGrpcClientModule<TClient>, ExternalGrpcClientModuleOptions<TClient>>(
            configure,
            optionsKey);
}
