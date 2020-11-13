using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Health.Teams
{
    public class TeamsHealthReporterModule : BaseApplicationModule<TeamsHealthCheckPublisherOptions>
    {
        public TeamsHealthReporterModule(TeamsHealthCheckPublisherOptions config,
            Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.Configure<HealthCheckPublisherOptions>(options => { });
            services.AddHealthChecks();
            services.AddHttpClient();
            services.AddSingleton<IHealthCheckPublisher, TeamsHealthCheckPublisher>();
        }

        public override void CheckConfig()
        {
            base.CheckConfig();
            if (string.IsNullOrEmpty(Config.WebHookUrl))
            {
                throw new ArgumentException("Teams web hooke url can't be empty", nameof(Config.WebHookUrl));
            }
        }
    }
}
