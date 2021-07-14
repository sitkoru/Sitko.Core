using FluentEmail.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sitko.Core.Email.Smtp
{
    public class SmtpEmailModule : FluentEmailModule<SmtpEmailModuleOptions>
    {
        protected override void ConfigureBuilder(FluentEmailServicesBuilder builder,
            SmtpEmailModuleOptions fluentEmailModuleOptions) =>
            builder.Services.TryAddScoped<ISender, MailKitSender>();

        public override string OptionsKey => "Email:Smtp";
    }
}
