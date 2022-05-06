using System;
using Sitko.Core.App;

namespace Sitko.Core.Health.Telegram;

public static class ApplicationExtensions
{
    public static Application AddTelegramHealthReporter(this Application application,
        Action<IApplicationContext, TelegramHealthReporterModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<TelegramHealthReporterModule, TelegramHealthReporterModuleOptions>(configure,
            optionsKey);

    public static Application AddTelegramHealthReporter(this Application application,
        Action<TelegramHealthReporterModuleOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<TelegramHealthReporterModule, TelegramHealthReporterModuleOptions>(configure,
            optionsKey);
}
