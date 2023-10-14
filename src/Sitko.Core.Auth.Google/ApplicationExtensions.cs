using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Auth.Google;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddGoogleAuth(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, GoogleAuthModuleOptions> configure, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddGoogleAuth(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddGoogleAuth(this IHostApplicationBuilder hostApplicationBuilder,
        Action<GoogleAuthModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddGoogleAuth(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddGoogleAuth(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, GoogleAuthModuleOptions> configure, string? optionsKey = null) =>
        applicationBuilder.AddModule<GoogleAuthModule, GoogleAuthModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddGoogleAuth(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<GoogleAuthModuleOptions>? configure = null, string? optionsKey = null) =>
        applicationBuilder.AddModule<GoogleAuthModule, GoogleAuthModuleOptions>(configure, optionsKey);
}
