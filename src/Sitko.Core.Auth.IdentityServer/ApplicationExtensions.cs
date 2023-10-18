using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Auth.IdentityServer;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddJwtIdentityServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, JwtIdentityServerModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore<ISitkoCoreWebApplicationBuilder>().AddJwtIdentityServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddJwtIdentityServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<JwtIdentityServerModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore<ISitkoCoreWebApplicationBuilder>().AddJwtIdentityServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddOidcIdentityServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, OidcIdentityServerModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore<ISitkoCoreWebApplicationBuilder>().AddOidcIdentityServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddOidcIdentityServer(this IHostApplicationBuilder hostApplicationBuilder,
        Action<OidcIdentityServerModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore<ISitkoCoreWebApplicationBuilder>().AddOidcIdentityServer(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreWebApplicationBuilder AddJwtIdentityServer(this ISitkoCoreWebApplicationBuilder applicationBuilder,
        Action<IApplicationContext, JwtIdentityServerModuleOptions> configure,
        string? optionsKey = null)
    {
        applicationBuilder
            .AddModule<JwtIdentityServerModule, JwtIdentityServerModuleOptions>(configure, optionsKey);
        return applicationBuilder;
    }

    public static ISitkoCoreWebApplicationBuilder AddJwtIdentityServer(this ISitkoCoreWebApplicationBuilder applicationBuilder,
        Action<JwtIdentityServerModuleOptions>? configure = null, string? optionsKey = null)
    {
        applicationBuilder
            .AddModule<JwtIdentityServerModule, JwtIdentityServerModuleOptions>(configure, optionsKey);
        return applicationBuilder;
    }

    public static ISitkoCoreWebApplicationBuilder AddOidcIdentityServer(this ISitkoCoreWebApplicationBuilder applicationBuilder,
        Action<IApplicationContext, OidcIdentityServerModuleOptions> configure,
        string? optionsKey = null)
    {
        applicationBuilder.AddModule<OidcIdentityServerModule, OidcIdentityServerModuleOptions>(configure,
            optionsKey);
        return applicationBuilder;
    }

    public static ISitkoCoreWebApplicationBuilder AddOidcIdentityServer(this ISitkoCoreWebApplicationBuilder applicationBuilder,
        Action<OidcIdentityServerModuleOptions>? configure = null, string? optionsKey = null)
    {
        applicationBuilder.AddModule<OidcIdentityServerModule, OidcIdentityServerModuleOptions>(configure,
            optionsKey);
        return applicationBuilder;
    }
}
