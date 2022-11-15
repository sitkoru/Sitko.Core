using Sitko.Core.App;

namespace Sitko.Core.Email.Smtp;

public static class ApplicationExtensions
{
    public static Application AddSmtpEmail(this Application application,
        Action<IApplicationContext, SmtpEmailModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<SmtpEmailModule, SmtpEmailModuleOptions>(configure, optionsKey);

    public static Application AddSmtpEmail(this Application application,
        Action<SmtpEmailModuleOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<SmtpEmailModule, SmtpEmailModuleOptions>(configure, optionsKey);
}

