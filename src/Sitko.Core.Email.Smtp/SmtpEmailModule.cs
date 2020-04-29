using System;
using FluentEmail.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            builder.Services.TryAdd(ServiceDescriptor.Scoped<ISender>(x => new MailKitSender(Config)));
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
}
