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
        where TClient : ClientBase<TClient>
    {
        var defaults = GetClientDefaults(applicationBuilder);
        return applicationBuilder.AddModule<GrpcClientModule<TClient>, GrpcClientModuleOptions<TClient>>(
            (context, options) =>
            {
                defaults.Apply(context, options);
                GrpcClientDefaultsModule.BindOptions(
                    context.Configuration.GetSection(optionsKey ?? $"Grpc:Client:Contracts:{GrpcServicesHelper.GetServiceNameForClient<TClient>()}"), options);
                configure(context, options);
            });
    }

    public static ISitkoCoreApplicationBuilder AddGrpcClient<TClient>(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<GrpcClientModuleOptions<TClient>>? configure = null,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        applicationBuilder.AddGrpcClient<TClient>((_, options) => configure?.Invoke(options), optionsKey);

    public static ISitkoCoreApplicationBuilder ConfigureGrpcClients(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, GrpcClientModuleOptions> configure)
    {
        GetClientDefaults(applicationBuilder).ConfigureActions.Add(configure);
        return applicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder ConfigureGrpcClients(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<GrpcClientModuleOptions> configure) =>
        applicationBuilder.ConfigureGrpcClients((_, options) => configure(options));

    public static IHostApplicationBuilder ConfigureGrpcClients(this IHostApplicationBuilder applicationBuilder,
        Action<IApplicationContext, GrpcClientModuleOptions> configure)
    {
        _ = applicationBuilder.GetSitkoCore().ConfigureGrpcClients(configure);
        return applicationBuilder;
    }

    public static IHostApplicationBuilder ConfigureGrpcClients(this IHostApplicationBuilder applicationBuilder,
        Action<GrpcClientModuleOptions> configure)
    {
        _ = applicationBuilder.GetSitkoCore().ConfigureGrpcClients(configure);
        return applicationBuilder;
    }

    private static GrpcClientDefaultsModule GetClientDefaults(ISitkoCoreApplicationBuilder applicationBuilder)
    {
        if (!applicationBuilder.HasModule<GrpcClientDefaultsModule>())
        {
            _ = applicationBuilder.AddModule<GrpcClientDefaultsModule>();
        }
        return applicationBuilder.Context.GetModuleInstance<GrpcClientDefaultsModule>();
    }

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
        applicationBuilder.AddGrpcClient<TClient>(
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
        applicationBuilder.AddGrpcClient<TClient>(
            options =>
            {
                options.UseGrpcWeb = true;
                configure?.Invoke(options);
            },
            optionsKey);
}
