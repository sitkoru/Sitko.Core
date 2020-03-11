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
                return new ConsulClient(config => { config.Address = options.ConsulUri; });
            });
            services.AddHealthChecks().AddConsul(options =>
            {
                options.HostName = Config.ConsulUri.Host;
                options.Port = Config.ConsulUri.Port;
                options.RequireHttps = false;
            });
        }
    }

    public class ConsulModuleConfig
    {
        public Uri ConsulUri { get; }

        public ConsulModuleConfig(Uri consulUri)
        {
            ConsulUri = consulUri;
        }
    }
}
