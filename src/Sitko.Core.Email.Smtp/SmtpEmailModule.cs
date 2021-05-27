using FluentEmail.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sitko.Core.Email.Smtp
{
    public class SmtpEmailModule : FluentEmailModule<SmtpEmailModuleConfig>
    {
        protected override void ConfigureBuilder(FluentEmailServicesBuilder builder,
            SmtpEmailModuleConfig fluentEmailModuleConfig)
        {
            builder.Services.TryAddScoped<ISender, MailKitSender>();
        }

        public override string GetConfigKey()
        {
            return "Email:Smtp";
        }
    }
}
