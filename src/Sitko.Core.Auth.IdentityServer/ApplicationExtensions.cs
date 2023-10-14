using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Auth.IdentityServer;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddJwtIdentityServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, JwtIdentityServerModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddJwtIdentityServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddJwtIdentityServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<JwtIdentityServerModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddJwtIdentityServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddOidcIdentityServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, OidcIdentityServerModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddOidcIdentityServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddOidcIdentityServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<OidcIdentityServerModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddOidcIdentityServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddJwtIdentityServer(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, JwtIdentityServerModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder
            .AddModule<JwtIdentityServerModule, JwtIdentityServerModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddJwtIdentityServer(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<JwtIdentityServerModuleOptions>? configure = null, string? optionsKey = null) =>
        applicationBuilder
            .AddModule<JwtIdentityServerModule, JwtIdentityServerModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddOidcIdentityServer(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, OidcIdentityServerModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<OidcIdentityServerModule, OidcIdentityServerModuleOptions>(configure,
            optionsKey);

    public static ISitkoCoreApplicationBuilder AddOidcIdentityServer(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<OidcIdentityServerModuleOptions>? configure = null, string? optionsKey = null) =>
        applicationBuilder.AddModule<OidcIdentityServerModule, OidcIdentityServerModuleOptions>(configure,
            optionsKey);
}
