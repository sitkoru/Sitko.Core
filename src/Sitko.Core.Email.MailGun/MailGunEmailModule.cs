using System;
using FluentEmail.Mailgun;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Email.MailGun
{
    public class MailGunEmailModule : FluentEmailModule<MailGunEmailModuleConfig>
    {
        public MailGunEmailModule(MailGunEmailModuleConfig config, Application application) : base(config, application)
        {
        }

        protected override void ConfigureBuilder(FluentEmailServicesBuilder builder)
        {
            builder.AddMailGunSender(Config.Domain, Config.ApiKey, Config.Region);
        }

        public override void CheckConfig()
        {
            base.CheckConfig();
            if (string.IsNullOrEmpty(Config.Domain))
            {
                throw new ArgumentException("Provide domain registered in mailgun", nameof(Config.Domain));
            }

            if (string.IsNullOrEmpty(Config.ApiKey))
            {
                throw new ArgumentException("Provide mailgun apikey", nameof(Config.ApiKey));
            }
        }
    }

    public class MailGunEmailModuleConfig : FluentEmailModuleConfig
    {
        public string Domain { get; set; } = "mg.localhost";
        public string ApiKey { get; set; } = string.Empty;
        public MailGunRegion Region { get; set; } = MailGunRegion.USA;
    }
}
