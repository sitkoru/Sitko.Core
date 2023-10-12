using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Search.ElasticSearch;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddElasticSearch(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, ElasticSearchModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddElasticSearch(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddElasticSearch(this IHostApplicationBuilder hostApplicationBuilder,
        Action<ElasticSearchModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddElasticSearch(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddElasticSearch(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ElasticSearchModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<ElasticSearchModule, ElasticSearchModuleOptions>(configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddElasticSearch(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<ElasticSearchModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<ElasticSearchModule, ElasticSearchModuleOptions>(configure, optionsKey);
}
