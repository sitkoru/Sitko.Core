using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Health.Telegram
{
    public class TelegramHealthReporterModule : BaseApplicationModule<TelegramHealthCheckPublisherOptions>
    {
        public override List<Type> GetRequiredModules()
        {
            return new List<Type> {typeof(HealthModule)};
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.Configure<HealthCheckPublisherOptions>(options => { });

            services.AddSingleton<IHealthCheckPublisher, TelegramHealthCheckPublisher>();
        }
    }
}
