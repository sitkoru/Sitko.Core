using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.NewRelic.Logging;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddNewRelicLogging(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, NewRelicLoggingModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddNewRelicLogging(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddNewRelicLogging(this IHostApplicationBuilder hostApplicationBuilder,
        Action<NewRelicLoggingModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddNewRelicLogging(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddNewRelicLogging(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, NewRelicLoggingModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<NewRelicLoggingModule, NewRelicLoggingModuleOptions>(configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddNewRelicLogging(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<NewRelicLoggingModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<NewRelicLoggingModule, NewRelicLoggingModuleOptions>(configure, optionsKey);
}
