using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Email.MailGun;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddMailGunEmail(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, MailGunEmailModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddMailGunEmail(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddMailGunEmail(this IHostApplicationBuilder hostApplicationBuilder,
        Action<MailGunEmailModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddMailGunEmail(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddMailGunEmail(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, MailGunEmailModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<MailGunEmailModule, MailGunEmailModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddMailGunEmail(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<MailGunEmailModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<MailGunEmailModule, MailGunEmailModuleOptions>(configure, optionsKey);
}
