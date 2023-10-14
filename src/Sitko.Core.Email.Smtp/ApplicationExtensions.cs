using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Email.Smtp;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddSmtpEmail(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, SmtpEmailModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddSmtpEmail(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddSmtpEmail(this IHostApplicationBuilder hostApplicationBuilder,
        Action<SmtpEmailModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddSmtpEmail(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddSmtpEmail(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, SmtpEmailModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<SmtpEmailModule, SmtpEmailModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddSmtpEmail(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<SmtpEmailModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<SmtpEmailModule, SmtpEmailModuleOptions>(configure, optionsKey);
}
