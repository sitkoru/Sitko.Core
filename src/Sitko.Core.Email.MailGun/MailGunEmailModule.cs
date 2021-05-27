using System.Collections.Generic;
using FluentEmail.Mailgun;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Email.MailGun
{
    public class MailGunEmailModule : FluentEmailModule<MailGunEmailModuleConfig>
    {
        protected override void ConfigureBuilder(FluentEmailServicesBuilder builder,
            MailGunEmailModuleConfig config)
        {
            builder.AddMailGunSender(config.Domain, config.ApiKey, config.Region);
        }

        public override string GetConfigKey()
        {
            return "Email:Mailgun";
        }
    }

    public class MailGunEmailModuleConfig : FluentEmailModuleConfig
    {
        public string Domain { get; set; } = "mg.localhost";
        public string ApiKey { get; set; } = string.Empty;
        public MailGunRegion Region { get; set; } = MailGunRegion.USA;

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(Domain))
                {
                    return (false, new[] {"Provide domain registered in mailgun"});
                }

                if (string.IsNullOrEmpty(ApiKey))
                {
                    return (false, new[] {"Provide mailgun apikey"});
                }
            }

            return result;
        }
    }
}
