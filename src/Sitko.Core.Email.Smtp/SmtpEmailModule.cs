using System;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Email.Smtp
{
    public class SmtpEmailModule : FluentEmailModule<SmtpEmailModuleConfig>
    {
        public SmtpEmailModule(SmtpEmailModuleConfig config, Application application) : base(config, application)
        {
        }

        protected override void ConfigureBuilder(FluentEmailServicesBuilder builder)
        {
            NetworkCredential? credential = null;
            if (!string.IsNullOrEmpty(Config.UserName))
            {
                credential = new NetworkCredential(Config.UserName, Config.Password);
            }

            builder.AddSmtpSender(() =>
            {
                var client = new SmtpClient(Config.Server, Config.Port) {EnableSsl = Config.UseSsl};
                if (credential != null)
                {
                    client.Credentials = credential;
                }

                return client;
            });
        }

        public override void CheckConfig()
        {
            base.CheckConfig();
            if (string.IsNullOrEmpty(Config.Server))
            {
                throw new ArgumentException("Provide smtp server", nameof(Config.Server));
            }

            if (Config.Port == 0)
            {
                throw new ArgumentException("Provide smtp port", nameof(Config.Port));
            }
        }
    }

    public class SmtpEmailModuleConfig : FluentEmailModuleConfig
    {
        public int Port { get; set; } = 25;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool UseSsl { get; set; } = false;
        public string Server { get; set; } = "localhost";
    }
}
