using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Search.OpenSearch;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddOpenSearch(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, OpenSearchModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddOpenSearch(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddOpenSearch(this IHostApplicationBuilder hostApplicationBuilder,
        Action<OpenSearchModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddOpenSearch(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddOpenSearch(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, OpenSearchModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<OpenSearchModule, OpenSearchModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddOpenSearch(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<OpenSearchModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<OpenSearchModule, OpenSearchModuleOptions>(configure, optionsKey);
}
