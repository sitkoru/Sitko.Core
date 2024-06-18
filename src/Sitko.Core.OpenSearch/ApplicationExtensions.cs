using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.OpenSearch;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddOpenSearchLogging(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, OpenSearchLoggingModuleOptions> configure, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddOpenSearchLogging(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddOpenSearchLogging(this IHostApplicationBuilder hostApplicationBuilder,
        Action<OpenSearchLoggingModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddOpenSearchLogging(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddOpenSearchLogging(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, OpenSearchLoggingModuleOptions> configure, string? optionsKey = null) =>
        applicationBuilder.AddModule<OpenSearchLoggingModule, OpenSearchLoggingModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddOpenSearchLogging(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<OpenSearchLoggingModuleOptions>? configure = null, string? optionsKey = null) =>
        applicationBuilder.AddModule<OpenSearchLoggingModule, OpenSearchLoggingModuleOptions>(configure, optionsKey);
}
