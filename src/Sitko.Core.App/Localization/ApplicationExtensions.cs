using JetBrains.Annotations;

namespace Sitko.Core.App.Localization;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddJsonLocalization(this Application application,
        Action<IApplicationContext, JsonLocalizationModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<JsonLocalizationModule, JsonLocalizationModuleOptions>(configure, optionsKey);

    public static Application AddJsonLocalization(this Application application,
        Action<JsonLocalizationModuleOptions>? configure = null, string? optionsKey = null) =>
        application.AddModule<JsonLocalizationModule, JsonLocalizationModuleOptions>(configure, optionsKey);
}

