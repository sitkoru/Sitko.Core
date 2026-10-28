using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Client;

public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddGrpcClient<TClient>(this IHostApplicationBuilder applicationBuilder,
        Action<IApplicationContext, GrpcClientModuleOptions<TClient>> configure,
        string? optionsKey = null)
        where TClient : ClientBase<TClient>
    {
        applicationBuilder.GetSitkoCore().AddGrpcClient(configure, optionsKey);
        return applicationBuilder;
    }

    public static IHostApplicationBuilder AddGrpcClient<TClient>(this IHostApplicationBuilder applicationBuilder,
        Action<GrpcClientModuleOptions<TClient>>? configure = null,
        string? optionsKey = null)
        where TClient : ClientBase<TClient>
    {
        applicationBuilder.GetSitkoCore().AddGrpcClient(configure, optionsKey);
        return applicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddGrpcClient<TClient>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, GrpcClientModuleOptions<TClient>> configure,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        applicationBuilder
            .AddModule<GrpcClientModule<TClient>, GrpcClientModuleOptions<TClient>>(
                configure,
                optionsKey);

    public static ISitkoCoreApplicationBuilder AddGrpcClient<TClient>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<GrpcClientModuleOptions<TClient>>? configure = null,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        applicationBuilder
            .AddModule<GrpcClientModule<TClient>, GrpcClientModuleOptions<TClient>>(
                configure,
                optionsKey);

    public static IHostApplicationBuilder AddGrpcWebClient<TClient>(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, GrpcClientModuleOptions<TClient>> configure,
        string? optionsKey = null)
        where TClient : ClientBase<TClient>
    {
        hostApplicationBuilder.GetSitkoCore().AddGrpcWebClient(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddGrpcWebClient<TClient>(this IHostApplicationBuilder hostApplicationBuilder,
        Action<GrpcClientModuleOptions<TClient>>? configure = null,
        string? optionsKey = null)
        where TClient : ClientBase<TClient>
    {
        hostApplicationBuilder.GetSitkoCore().AddGrpcWebClient(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddGrpcWebClient<TClient>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, GrpcClientModuleOptions<TClient>> configure,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        applicationBuilder.AddModule<GrpcClientModule<TClient>, GrpcClientModuleOptions<TClient>>(
            (context, options) =>
            {
                options.UseGrpcWeb = true;
                configure(context, options);
            },
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddGrpcWebClient<TClient>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<GrpcClientModuleOptions<TClient>>? configure = null,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        applicationBuilder.AddModule<GrpcClientModule<TClient>, GrpcClientModuleOptions<TClient>>(
            options =>
            {
                options.UseGrpcWeb = true;
                configure?.Invoke(options);
            },
            optionsKey);
}
