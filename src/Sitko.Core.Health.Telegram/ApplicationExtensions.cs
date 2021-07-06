using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Health.Telegram
{
    public static class ApplicationExtensions
    {
        public static Application AddTelegramHealthReporter(this Application application,
            Action<IConfiguration, IHostEnvironment, TelegramHealthReporterModuleOptions> configure,
            string? optionsKey = null)
        {
            return application.AddModule<TelegramHealthReporterModule, TelegramHealthReporterModuleOptions>(configure,
                optionsKey);
        }

        public static Application AddTelegramHealthReporter(this Application application,
            Action<TelegramHealthReporterModuleOptions>? configure = null,
            string? optionsKey = null)
        {
            return application.AddModule<TelegramHealthReporterModule, TelegramHealthReporterModuleOptions>(configure,
                optionsKey);
        }
    }
}
