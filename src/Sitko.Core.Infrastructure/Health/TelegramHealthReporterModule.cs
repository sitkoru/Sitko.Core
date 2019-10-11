using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Infrastructure.Health
{
    public class TelegramHealthReporterModule : BaseApplicationModule<TelegramHealthCheckPublisherOptions>
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddHealthChecks();
            services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromSeconds(2);
            });

            services.AddSingleton<IHealthCheckPublisher, TelegramHealthCheckPublisher>();
        }
    }
}
