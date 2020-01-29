using App.Metrics;
using App.Metrics.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Metrics
{
    public class MetricsModule : BaseApplicationModule
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            var metricsBuilder = AppMetrics.CreateDefaultBuilder();

            metricsBuilder
                .OutputMetrics.AsPrometheusPlainText()
                .Configuration.Configure(options =>
                {
                    options.DefaultContextLabel = environment.ApplicationName;
                    options.GlobalTags["env"] = environment.IsStaging()
                        ? "stage"
                        : environment.IsProduction()
                            ? "prod"
                            : "dev";
                });

            metricsBuilder.Configuration.ReadFrom(configuration);

            if (metricsBuilder.CanReport())
            {
                services.AddMetricsReportingHostedService();
            }

            services.AddMetrics(metricsBuilder);
            services.AddSingleton<IMetricsCollector, MetricsCollector>();
        }
    }
    
    public static class MetricsModuleExtensions
    {
        public static T AddMetrics<T>(this T application) where T : Application<T>
        {
            return application.AddModule<MetricsModule>();
        }
    }
}
