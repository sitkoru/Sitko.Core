using FluentEmail.Mailgun;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Email.MailGun
{
    public class MailGunEmailModule : FluentEmailModule<MailGunEmailModuleOptions>
    {
        protected override void ConfigureBuilder(FluentEmailServicesBuilder builder,
            MailGunEmailModuleOptions options)
        {
            builder.AddMailGunSender(options.Domain, options.ApiKey, options.Region);
        }

        public override string GetOptionsKey()
        {
            return "Email:Mailgun";
        }
    }

    public class MailGunEmailModuleOptions : FluentEmailModuleOptions
    {
        public string Domain { get; set; } = "mg.localhost";
        public string ApiKey { get; set; } = string.Empty;
        public MailGunRegion Region { get; set; } = MailGunRegion.USA;
    }

    public class MailGunEmailModuleOptionsValidator : FluentEmailModuleOptionsValidator<MailGunEmailModuleOptions>
    {
        public MailGunEmailModuleOptionsValidator()
        {
            RuleFor(o => o.Domain).NotEmpty().WithMessage("Provide domain registered in mailgun");
            RuleFor(o => o.ApiKey).NotEmpty().WithMessage("Provide mailgun apikey");
        }
    }
}
