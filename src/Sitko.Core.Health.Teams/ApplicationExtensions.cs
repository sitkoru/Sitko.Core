using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Health.Teams
{
    public static class ApplicationExtensions
    {
        public static Application AddTeamsHealthReporter(this Application application,
            Action<IConfiguration, IHostEnvironment, TeamsHealthReporterModuleOptions> configure,
            string? optionsKey = null)
        {
            return application.AddModule<TeamsHealthReporterModule, TeamsHealthReporterModuleOptions>(configure,
                optionsKey);
        }

        public static Application AddTeamsHealthReporter(this Application application,
            Action<TeamsHealthReporterModuleOptions>? configure = null, string? optionsKey = null)
        {
            return application.AddModule<TeamsHealthReporterModule, TeamsHealthReporterModuleOptions>(configure,
                optionsKey);
        }
    }
}
