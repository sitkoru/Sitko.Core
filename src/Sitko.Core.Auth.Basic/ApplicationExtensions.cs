using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Auth.Basic;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddBasicAuth(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, BasicAuthModuleOptions> configure, string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddBasicAuth(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddBasicAuth(this IHostApplicationBuilder hostApplicationBuilder,
        Action<BasicAuthModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddBasicAuth(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddBasicAuth(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, BasicAuthModuleOptions> configure, string? optionsKey = null) =>
        applicationBuilder.AddModule<BasicAuthModule, BasicAuthModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddBasicAuth(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<BasicAuthModuleOptions>? configure = null, string? optionsKey = null) =>
        applicationBuilder.AddModule<BasicAuthModule, BasicAuthModuleOptions>(configure, optionsKey);
}
