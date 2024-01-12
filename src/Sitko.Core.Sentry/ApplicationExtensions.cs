using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Sentry;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddSentry(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, SentryModuleOptions> configure, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddSentry(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddSentry(this IHostApplicationBuilder hostApplicationBuilder,
        Action<SentryModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddSentry(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddSentry(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, SentryModuleOptions> configure, string? optionsKey = null) =>
        applicationBuilder.AddModule<SentryModule, SentryModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddSentry(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<SentryModuleOptions>? configure = null, string? optionsKey = null) =>
        applicationBuilder.AddModule<SentryModule, SentryModuleOptions>(configure, optionsKey);
}
