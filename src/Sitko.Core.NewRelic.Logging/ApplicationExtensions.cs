using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.NewRelic.Logging
{
    public static class ApplicationExtensions
    {
        public static Application AddNewRelicLogging(this Application application,
            Action<IConfiguration, IHostEnvironment, NewRelicLoggingModuleOptions> configure,
            string? optionsKey = null)
        {
            return application.AddModule<NewRelicLoggingModule, NewRelicLoggingModuleOptions>(configure, optionsKey);
        }

        public static Application AddNewRelicLogging(this Application application,
            Action<NewRelicLoggingModuleOptions>? configure = null,
            string? optionsKey = null)
        {
            return application.AddModule<NewRelicLoggingModule, NewRelicLoggingModuleOptions>(configure, optionsKey);
        }
    }
}
