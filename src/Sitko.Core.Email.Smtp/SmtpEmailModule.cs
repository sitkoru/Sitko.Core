using System;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Email.Smtp
{
    public class SmtpEmailModule : FluentEmailModule<SmtpEmailModuleConfig>
    {
        protected override void ConfigureBuilder(FluentEmailServicesBuilder builder)
        {
            var client = new SmtpClient(Config.Server, Config.Port) {EnableSsl = Config.UseSsl};
            if (!string.IsNullOrEmpty(Config.UserName))
            {
                client.Credentials = new NetworkCredential(Config.UserName, Config.Password);
            }

            builder.AddSmtpSender(client);
        }
    }

    public class SmtpEmailModuleConfig : FluentEmailModuleConfig
    {
        public SmtpEmailModuleConfig(string server, int port, string userName, string password, bool useSsl,
            string from,
            string host,
            string scheme) : base(
            from, host, scheme)
        {
            if (string.IsNullOrEmpty(server))
            {
                throw new ArgumentException("Provide smtp server", nameof(server));
            }

            Server = server;

            if (port == 0)
            {
                throw new ArgumentException("Provide smtp port", nameof(port));
            }

            Port = port;
            UserName = userName;
            Password = password;
            UseSsl = useSsl;
        }

        public int Port { get; }
        public string UserName { get; }
        public string Password { get; }
        public bool UseSsl { get; }
        public string Server { get; }
    }
}
