using System;
using Sitko.Core.App;

namespace Sitko.Core.Email.MailGun;

public static class ApplicationExtensions
{
    public static Application AddMailGunEmail(this Application application,
        Action<IApplicationContext, MailGunEmailModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<MailGunEmailModule, MailGunEmailModuleOptions>(configure, optionsKey);

    public static Application AddMailGunEmail(this Application application,
        Action<MailGunEmailModuleOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<MailGunEmailModule, MailGunEmailModuleOptions>(configure, optionsKey);
}
