using System;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Consul
{
    public class ConsulModule : BaseApplicationModule<ConsulModuleConfig>
    {
        public ConsulModule(ConsulModuleConfig config, Application application) : base(config,
            application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IConsulClient, ConsulClient>(p =>
            {
                var options = p.GetRequiredService<ConsulModuleConfig>();
                return new ConsulClient(config => { config.Address = new Uri(options.ConsulUri); });
            });
            services.AddHealthChecks().AddConsul(options =>
            {
                var uri = new Uri(Config.ConsulUri);
                options.HostName = uri.Host;
                options.Port = uri.Port;
                options.RequireHttps = false;
            });
        }
    }

    public class ConsulModuleConfig
    {
        public string ConsulUri { get; }

        public ConsulModuleConfig(string consulUri)
        {
            ConsulUri = consulUri;
        }
    }
}
