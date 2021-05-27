using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sitko.Core.App;

namespace Sitko.Core.Health.Teams
{
    public class TeamsHealthReporterModule : BaseApplicationModule<TeamsHealthCheckPublisherOptions>
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TeamsHealthCheckPublisherOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.Configure<HealthCheckPublisherOptions>(_ => { });
            services.AddHealthChecks();
            services.AddHttpClient();
            services.AddSingleton<IHealthCheckPublisher, TeamsHealthCheckPublisher>();
        }

        public override string GetOptionsKey()
        {
            return "Health:Teams";
        }
    }
}
