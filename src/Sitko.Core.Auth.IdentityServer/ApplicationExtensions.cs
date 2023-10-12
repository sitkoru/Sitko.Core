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
        hostApplicationBuilder.AddSitkoCore().AddJwtIdentityServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddJwtIdentityServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<JwtIdentityServerModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddJwtIdentityServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddOidcIdentityServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, OidcIdentityServerModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddOidcIdentityServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddOidcIdentityServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<OidcIdentityServerModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddOidcIdentityServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddJwtIdentityServer(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, JwtIdentityServerModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder
            .AddModule<JwtIdentityServerModule, JwtIdentityServerModuleOptions>(configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddJwtIdentityServer(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<JwtIdentityServerModuleOptions>? configure = null, string? optionsKey = null) =>
        applicationBuilder
            .AddModule<JwtIdentityServerModule, JwtIdentityServerModuleOptions>(configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddOidcIdentityServer(this SitkoCoreApplicationBuilder application,
        Action<IApplicationContext, OidcIdentityServerModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<OidcIdentityServerModule, OidcIdentityServerModuleOptions>(configure,
            optionsKey);

    public static SitkoCoreApplicationBuilder AddOidcIdentityServer(this SitkoCoreApplicationBuilder application,
        Action<OidcIdentityServerModuleOptions>? configure = null, string? optionsKey = null) =>
        application.AddModule<OidcIdentityServerModule, OidcIdentityServerModuleOptions>(configure,
            optionsKey);
}
