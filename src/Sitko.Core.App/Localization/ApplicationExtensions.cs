using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App.Localization;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddJsonLocalization(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, JsonLocalizationModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddJsonLocalization(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddJsonLocalization(this IHostApplicationBuilder hostApplicationBuilder,
        Action<JsonLocalizationModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddJsonLocalization(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddJsonLocalization(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, JsonLocalizationModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<JsonLocalizationModule, JsonLocalizationModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddJsonLocalization(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<JsonLocalizationModuleOptions>? configure = null, string? optionsKey = null) =>
        applicationBuilder.AddModule<JsonLocalizationModule, JsonLocalizationModuleOptions>(configure, optionsKey);
}
