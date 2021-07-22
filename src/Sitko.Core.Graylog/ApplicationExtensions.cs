using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Graylog
{
    public static class ApplicationExtensions
    {
        public static Application AddGraylog(this Application application,
            Action<IConfiguration, IHostEnvironment, GraylogModuleOptions> configure, string? optionsKey = null) =>
            application.AddModule<GraylogModule, GraylogModuleOptions>(configure, optionsKey);

        public static Application AddGraylog(this Application application,
            Action<GraylogModuleOptions>? configure = null, string? optionsKey = null) =>
            application.AddModule<GraylogModule, GraylogModuleOptions>(configure, optionsKey);
    }
}
