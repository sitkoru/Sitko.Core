using System;
using Sitko.Core.App;

namespace Sitko.Core.ElasticStack;

public static class ApplicationExtensions
{
    public static Application AddElasticStack(this Application application,
        Action<IApplicationContext, ElasticStackModuleOptions> configure, string? optionsKey = null) =>
        application.AddModule<ElasticStackModule, ElasticStackModuleOptions>(configure, optionsKey);

    public static Application AddElasticStack(this Application application,
        Action<ElasticStackModuleOptions>? configure = null, string? optionsKey = null) =>
        application.AddModule<ElasticStackModule, ElasticStackModuleOptions>(configure, optionsKey);
}
