using Sitko.Core.App;

namespace Sitko.Core.Sentry;

public static class ApplicationExtensions
{
    public static Application AddSentry(this Application application,
        Action<IApplicationContext, SentryModuleOptions> configure, string? optionsKey = null) =>
        application.AddModule<SentryModule, SentryModuleOptions>(configure, optionsKey);

    public static Application AddSentry(this Application application,
        Action<SentryModuleOptions>? configure = null, string? optionsKey = null) =>
        application.AddModule<SentryModule, SentryModuleOptions>(configure, optionsKey);
}
