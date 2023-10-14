using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Health.Teams;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddTeamsHealthReporter(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, TeamsHealthReporterModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddTeamsHealthReporter(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddTeamsHealthReporter(this IHostApplicationBuilder hostApplicationBuilder,
        Action<TeamsHealthReporterModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddTeamsHealthReporter(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddTeamsHealthReporter(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, TeamsHealthReporterModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<TeamsHealthReporterModule, TeamsHealthReporterModuleOptions>(configure,
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddTeamsHealthReporter(
        this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<TeamsHealthReporterModuleOptions>? configure = null, string? optionsKey = null) =>
        applicationBuilder.AddModule<TeamsHealthReporterModule, TeamsHealthReporterModuleOptions>(configure,
            optionsKey);
}
