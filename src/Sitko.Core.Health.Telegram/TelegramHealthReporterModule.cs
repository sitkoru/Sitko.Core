using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Health.Telegram
{
    public class TelegramHealthReporterModule : BaseApplicationModule<TelegramHealthCheckPublisherOptions>
    {
        public TelegramHealthReporterModule(TelegramHealthCheckPublisherOptions config,
            Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.Configure<HealthCheckPublisherOptions>(options => { });
            services.AddHealthChecks();
            services.AddSingleton<IHealthCheckPublisher, TelegramHealthCheckPublisher>();
        }

        public override void CheckConfig()
        {
            base.CheckConfig();
            if (string.IsNullOrEmpty(Config.Token))
            {
                throw new ArgumentException("Telegram token can't be empty", nameof(Config.Token));
            }

            if (Config.ChatId == 0)
            {
                throw new ArgumentException("Telegram chat id can't be 0", nameof(Config.ChatId));
            }
        }
    }
}
