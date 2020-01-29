using System;
using FluentEmail.Mailgun;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Email.MailGun
{
    public class MailGunEmailModule : FluentEmailModule<MailGunEmailModuleConfig>
    {
        protected override void ConfigureBuilder(FluentEmailServicesBuilder builder)
        {
            builder.AddMailGunSender(Config.Domain, Config.ApiKey, Config.Region);
        }
    }

    public class MailGunEmailModuleConfig : FluentEmailModuleConfig
    {
        public MailGunEmailModuleConfig(string domain, string apiKey, MailGunRegion region, string from, string host,
            string scheme) : base(
            from, host, scheme)
        {
            if (string.IsNullOrEmpty(domain))
            {
                throw new ArgumentException("Provide domain registered in mailgun", nameof(domain));
            }

            Domain = domain;

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("Provide mailgun apikey", nameof(apiKey));
            }

            ApiKey = apiKey;
            Region = region;
        }

        public string Domain { get; }
        public string ApiKey { get; }
        public MailGunRegion Region { get; }
    }
}
