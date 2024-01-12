using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Graylog;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddGraylog(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, GraylogModuleOptions> configure, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddGraylog(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddGraylog(this IHostApplicationBuilder hostApplicationBuilder,
        Action<GraylogModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddGraylog(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddGraylog(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, GraylogModuleOptions> configure, string? optionsKey = null) =>
        applicationBuilder.AddModule<GraylogModule, GraylogModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddGraylog(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<GraylogModuleOptions>? configure = null, string? optionsKey = null) =>
        applicationBuilder.AddModule<GraylogModule, GraylogModuleOptions>(configure, optionsKey);
}
