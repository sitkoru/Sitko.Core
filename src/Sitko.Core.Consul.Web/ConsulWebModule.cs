using System;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sitko.Core.App;

namespace Sitko.Core.Consul.Web
{
    public class ConsulWebModule : ConsulModule<ConsulWebModuleOptions>
    {
        public override string OptionsKey => "Consul:Web";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            ConsulWebModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddSingleton<ConsulWebClient>();
            services.AddHealthChecks()
                .AddCheck<ConsulWebHealthCheck>("Consul registration")
                .AddConsul(options =>
                {
                    var uri = new Uri(startupOptions.ConsulUri);
                    options.HostName = uri.Host;
                    options.Port = uri.Port;
                    options.RequireHttps = false;
                });
        }

        public override Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            var client = serviceProvider.GetRequiredService<ConsulWebClient>();
            return client.RegisterAsync();
        }

        public override async Task ApplicationStopping(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            var consulClient = serviceProvider.GetRequiredService<IConsulClient>();
            var logger = serviceProvider.GetRequiredService<ILogger<ConsulWebModule>>();
            logger.LogInformation("Remove service from Consul");
            await consulClient.Agent.ServiceDeregister(environment.ApplicationName);
        }
    }
}
