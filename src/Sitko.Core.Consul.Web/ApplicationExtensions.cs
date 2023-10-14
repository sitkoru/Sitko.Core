using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Consul.Web;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddConsulWeb(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, ConsulWebModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddConsulWeb(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddConsulWeb(this IHostApplicationBuilder hostApplicationBuilder,
        Action<ConsulWebModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddConsulWeb(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddConsulWeb(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ConsulWebModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<ConsulWebModule, ConsulWebModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddConsulWeb(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<ConsulWebModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<ConsulWebModule, ConsulWebModuleOptions>(configure, optionsKey);
}
