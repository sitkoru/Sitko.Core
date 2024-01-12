using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Consul;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddConsul(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, ConsulModuleOptions> configure, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddConsul(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddConsul(this IHostApplicationBuilder hostApplicationBuilder,
        Action<ConsulModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddConsul(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddConsul(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, ConsulModuleOptions> configure, string? optionsKey = null) =>
        applicationBuilder.AddModule<ConsulModule, ConsulModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddConsul(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<ConsulModuleOptions>? configure = null, string? optionsKey = null) =>
        applicationBuilder.AddModule<ConsulModule, ConsulModuleOptions>(configure, optionsKey);
}
