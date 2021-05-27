using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sitko.Core.App;

namespace Sitko.Core.Health.Telegram
{
    public class TelegramHealthReporterModule : BaseApplicationModule<TelegramHealthCheckPublisherOptions>
    {
        public override string GetConfigKey()
        {
            return "Health:Telegram";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TelegramHealthCheckPublisherOptions startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.Configure<HealthCheckPublisherOptions>(_ => { });
            services.AddHealthChecks();
            services.AddHttpClient();
            services.AddSingleton<IHealthCheckPublisher, TelegramHealthCheckPublisher>();
        }
    }
}
