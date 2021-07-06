using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App.Localization
{
    public static class ApplicationExtensions
    {
        public static Application AddJsonLocalization(this Application application,
            Action<IConfiguration, IHostEnvironment, JsonLocalizationModuleOptions> configure,
            string? optionsKey = null)
        {
            return application.AddModule<JsonLocalizationModule, JsonLocalizationModuleOptions>(configure, optionsKey);
        }

        public static Application AddJsonLocalization(this Application application,
            Action<JsonLocalizationModuleOptions>? configure = null, string? optionsKey = null)
        {
            return application.AddModule<JsonLocalizationModule, JsonLocalizationModuleOptions>(configure, optionsKey);
        }
    }
}
