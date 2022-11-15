using FluentEmail.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sitko.Core.Email.Smtp;

public class SmtpEmailModule : FluentEmailModule<SmtpEmailModuleOptions>
{
    public override string OptionsKey => "Email:Smtp";

    protected override void ConfigureBuilder(FluentEmailServicesBuilder builder,
        SmtpEmailModuleOptions moduleOptions) =>
        builder.Services.TryAddScoped<ISender, MailKitSender>();
}

