using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Email.Smtp;

public class SmtpEmailModule : FluentEmailModule<SmtpEmailModuleOptions>
{
    public override string OptionsKey => "Email:Smtp";

    protected override void ConfigureBuilder(FluentEmailServicesBuilder builder,
        SmtpEmailModuleOptions moduleOptions) =>
        builder.AddSmtpSender(() =>
        {
            var client = new SmtpClient(moduleOptions.Server)
            {
                Port = moduleOptions.Port, EnableSsl = moduleOptions.EnableSsl
            };

            if (!string.IsNullOrEmpty(moduleOptions.UserName))
            {
                client.Credentials = new NetworkCredential(moduleOptions.UserName, moduleOptions.Password);
            }

            return client;
        });
}
