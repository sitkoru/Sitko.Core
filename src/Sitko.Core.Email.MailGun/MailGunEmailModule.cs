using System;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Email.MailGun
{
    public class MailGunEmailModule : EmailModule<MailGunEmailModuleConfig>
    {
        protected override void ConfigureBuilder(FluentEmailServicesBuilder builder)
        {
            builder.AddMailGunSender(Config.Domain, Config.ApiKey);
        }
    }

    public class MailGunEmailModuleConfig : EmailModuleConfig
    {
        public MailGunEmailModuleConfig(string domain, string apiKey, string from, string host, string scheme) : base(
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
        }

        public string Domain { get; }
        public string ApiKey { get; }
    }
}
