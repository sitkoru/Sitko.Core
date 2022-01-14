using System;
using Sitko.Core.App;

namespace Sitko.Core.NewRelic.Logging;

public static class ApplicationExtensions
{
    public static Application AddNewRelicLogging(this Application application,
        Action<IApplicationContext, NewRelicLoggingModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<NewRelicLoggingModule, NewRelicLoggingModuleOptions>(configure, optionsKey);

    public static Application AddNewRelicLogging(this Application application,
        Action<NewRelicLoggingModuleOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<NewRelicLoggingModule, NewRelicLoggingModuleOptions>(configure, optionsKey);
}
