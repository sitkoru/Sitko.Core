using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.HangFire
{
    public static class ApplicationExtensions
    {
        public static Application AddHangfirePostgres(this Application application,
            Action<IConfiguration, IHostEnvironment, HangfirePostgresModuleOptions> configure,
            string? optionsKey = null) =>
            application.AddModule<HangfireModule<HangfirePostgresModuleOptions>, HangfirePostgresModuleOptions>(
                configure, optionsKey);

        public static Application AddHangfirePostgres(this Application application,
            Action<HangfirePostgresModuleOptions>? configure = null,
            string? optionsKey = null) =>
            application.AddModule<HangfireModule<HangfirePostgresModuleOptions>, HangfirePostgresModuleOptions>(
                configure, optionsKey);
    }
}
