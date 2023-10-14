using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.HangFire;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddHangfirePostgres(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, HangfirePostgresModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddHangfirePostgres(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddHangfirePostgres(this IHostApplicationBuilder hostApplicationBuilder,
        Action<HangfirePostgresModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddHangfirePostgres(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddHangfirePostgres(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, HangfirePostgresModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<HangfireModule<HangfirePostgresModuleOptions>, HangfirePostgresModuleOptions>(
            configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddHangfirePostgres(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<HangfirePostgresModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<HangfireModule<HangfirePostgresModuleOptions>, HangfirePostgresModuleOptions>(
            configure, optionsKey);
}
