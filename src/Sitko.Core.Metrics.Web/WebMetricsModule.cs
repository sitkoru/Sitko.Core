using System.Linq;
using App.Metrics.AspNetCore;
using App.Metrics.Formatters.Prometheus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Web;

namespace Sitko.Core.Metrics.Web
{
    public class WebMetricsModule : BaseApplicationModule, IWebApplicationModule
    {
        public void ConfigureBeforeUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            appBuilder.Use((context, next) =>
            {
                var metricsCurrentRouteName = "__App.Metrics.CurrentRouteName__";
                var endpointFeature = context.Features[typeof(IEndpointFeature)] as IEndpointFeature;
                if (endpointFeature?.Endpoint is RouteEndpoint endpoint)
                {
                    var method = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods
                        ?.FirstOrDefault();
                    var routePattern = endpoint.RoutePattern?.RawText;
                    var templateRoute = $"{method} {routePattern}";
                    if (!context.Items.ContainsKey(metricsCurrentRouteName))
                    {
                        context.Items.Add(metricsCurrentRouteName, templateRoute);
                    }
                }

                return next();
            });
        }

        public void ConfigureWebHost(IWebHostBuilder webHostBuilder)
        {
            webHostBuilder.UseMetrics<DefaultMetricsStartupFilter>(options =>
            {
                options.EndpointOptions = endpointsOptions =>
                {
                    endpointsOptions.MetricsTextEndpointOutputFormatter =
                        new MetricsPrometheusTextOutputFormatter();
                };
            });
        }

        public void ConfigureEndpoints(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
        {
        }

        public void ConfigureAfterUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
        }

        public void ConfigureWebHostDefaults(IWebHostBuilder webHostBuilder)
        {
            // TODO: Remove when https://github.com/AppMetrics/AppMetrics/issues/396 is fixed
            webHostBuilder.ConfigureKestrel(options =>
            {
                options.AllowSynchronousIO = true;
            });
        }
    }
}
