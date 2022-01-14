using System;
using Sitko.Core.App;

namespace Sitko.Core.Health.Teams;

public static class ApplicationExtensions
{
    public static Application AddTeamsHealthReporter(this Application application,
        Action<IApplicationContext, TeamsHealthReporterModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<TeamsHealthReporterModule, TeamsHealthReporterModuleOptions>(configure,
            optionsKey);

    public static Application AddTeamsHealthReporter(this Application application,
        Action<TeamsHealthReporterModuleOptions>? configure = null, string? optionsKey = null) =>
        application.AddModule<TeamsHealthReporterModule, TeamsHealthReporterModuleOptions>(configure,
            optionsKey);
}
