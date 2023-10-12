using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.ElasticStack;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddElasticStack(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, ElasticStackModuleOptions> configure, string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddElasticStack(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddElasticStack(this IHostApplicationBuilder hostApplicationBuilder,
        Action<ElasticStackModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddElasticStack(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddElasticStack(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ElasticStackModuleOptions> configure, string? optionsKey = null) =>
        applicationBuilder.AddModule<ElasticStackModule, ElasticStackModuleOptions>(configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddElasticStack(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<ElasticStackModuleOptions>? configure = null, string? optionsKey = null) =>
        applicationBuilder.AddModule<ElasticStackModule, ElasticStackModuleOptions>(configure, optionsKey);
}
