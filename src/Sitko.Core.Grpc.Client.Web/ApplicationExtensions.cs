using Grpc.Core;
using Grpc.Net.Client.Web;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Grpc.Client.External;

namespace Sitko.Core.Grpc.Client.Web;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddGrpcWebClient<TClient>(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, ExternalGrpcClientModuleOptions<TClient>> configure,
        string? optionsKey = null)
        where TClient : ClientBase<TClient>
    {
        hostApplicationBuilder.GetSitkoCore().AddGrpcWebClient(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddGrpcWebClient<TClient>(this IHostApplicationBuilder hostApplicationBuilder,
        Action<ExternalGrpcClientModuleOptions<TClient>>? configure = null,
        string? optionsKey = null)
        where TClient : ClientBase<TClient>
    {
        hostApplicationBuilder.GetSitkoCore().AddGrpcWebClient(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddGrpcWebClient<TClient>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ExternalGrpcClientModuleOptions<TClient>> configure,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        applicationBuilder.AddModule<ExternalGrpcClientModule<TClient>, ExternalGrpcClientModuleOptions<TClient>>(
            (context, options) =>
            {
                options.ConfigureHttpHandler = handler => new GrpcWebHandler(handler);
                configure(context, options);
            },
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddGrpcWebClient<TClient>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<ExternalGrpcClientModuleOptions<TClient>>? configure = null,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        applicationBuilder.AddModule<ExternalGrpcClientModule<TClient>, ExternalGrpcClientModuleOptions<TClient>>(
            options =>
            {
                options.ConfigureHttpHandler = handler => new GrpcWebHandler(handler);
                configure?.Invoke(options);
            },
            optionsKey);
}
