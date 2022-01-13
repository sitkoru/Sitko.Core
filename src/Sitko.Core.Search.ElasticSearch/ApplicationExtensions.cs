using System;
using Microsoft.Extensions.Configuration;
using Sitko.Core.App;

namespace Sitko.Core.Search.ElasticSearch;

public static class ApplicationExtensions
{
    public static Application AddElasticSearch(this Application application,
        Action<IConfiguration, IAppEnvironment, ElasticSearchModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<ElasticSearchModule, ElasticSearchModuleOptions>(configure, optionsKey);

    public static Application AddElasticSearch(this Application application,
        Action<ElasticSearchModuleOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<ElasticSearchModule, ElasticSearchModuleOptions>(configure, optionsKey);
}
