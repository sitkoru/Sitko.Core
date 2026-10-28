using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Auth.OIDC;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddJwtAuth(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, JwtAuthModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore<ISitkoCoreWebApplicationBuilder>().AddJwtAuth(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddJwtAuth(this IHostApplicationBuilder hostApplicationBuilder,
        Action<JwtAuthModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore<ISitkoCoreWebApplicationBuilder>().AddJwtAuth(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddOidcAuth(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, OidcAuthModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore<ISitkoCoreWebApplicationBuilder>().AddOidcAuth(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddOidcAuth(this IHostApplicationBuilder hostApplicationBuilder,
        Action<OidcAuthModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore<ISitkoCoreWebApplicationBuilder>().AddOidcAuth(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreWebApplicationBuilder AddJwtAuth(this ISitkoCoreWebApplicationBuilder applicationBuilder,
        Action<IApplicationContext, JwtAuthModuleOptions> configure,
        string? optionsKey = null)
    {
        applicationBuilder
            .AddModule<JwtAuthModule, JwtAuthModuleOptions>(configure, optionsKey);
        return applicationBuilder;
    }

    public static ISitkoCoreWebApplicationBuilder AddJwtAuth(this ISitkoCoreWebApplicationBuilder applicationBuilder,
        Action<JwtAuthModuleOptions>? configure = null, string? optionsKey = null)
    {
        applicationBuilder
            .AddModule<JwtAuthModule, JwtAuthModuleOptions>(configure, optionsKey);
        return applicationBuilder;
    }

    public static ISitkoCoreWebApplicationBuilder AddOidcAuth(this ISitkoCoreWebApplicationBuilder applicationBuilder,
        Action<IApplicationContext, OidcAuthModuleOptions> configure,
        string? optionsKey = null)
    {
        applicationBuilder.AddModule<OidcAuthModule, OidcAuthModuleOptions>(configure,
            optionsKey);
        return applicationBuilder;
    }

    public static ISitkoCoreWebApplicationBuilder AddOidcAuth(this ISitkoCoreWebApplicationBuilder applicationBuilder,
        Action<OidcAuthModuleOptions>? configure = null, string? optionsKey = null)
    {
        applicationBuilder.AddModule<OidcAuthModule, OidcAuthModuleOptions>(configure,
            optionsKey);
        return applicationBuilder;
    }
}
