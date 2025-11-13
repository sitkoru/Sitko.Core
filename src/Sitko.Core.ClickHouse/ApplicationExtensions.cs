using System.Net;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Sitko.Core.App;

namespace Sitko.Core.ClickHouse;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddClickHouse(
        this IHostApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ClickHouseModuleOptions> configure,
        string? optionsKey = null)
    {
        applicationBuilder.GetSitkoCore().AddClickHouse(configure, optionsKey);
        return applicationBuilder;
    }

    public static IHostApplicationBuilder AddClickHouse(
        this IHostApplicationBuilder applicationBuilder,
        Action<ClickHouseModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        applicationBuilder.GetSitkoCore().AddClickHouse(configure, optionsKey);
        return applicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddClickHouse(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ClickHouseModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder
            .AddModule<ClickHouseModule, ClickHouseModuleOptions>(configure,
                optionsKey);

    public static ISitkoCoreApplicationBuilder AddClickHouse(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<ClickHouseModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder
            .AddModule<ClickHouseModule, ClickHouseModuleOptions>(configure,
                optionsKey);

    public static IServiceCollection AddClickhouseClient(this IServiceCollection services)
    {
        services.AddHttpClient(ClickHouseModule.HttpClientName).ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<ClickHouseModuleOptions>>();
            var httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                //MaxConnectionsPerServer = 1 will fix "session is locked", https://github.com/DarkWanderer/ClickHouse.Client/issues/236#issuecomment-2523069106
                MaxConnectionsPerServer = optionsMonitor.CurrentValue.MaxConnectionsPerServer,
            };
            if (optionsMonitor.CurrentValue.DisableCertificatesValidation)
            {
                httpClientHandler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            return httpClientHandler;
        });
        return services;
    }
}
