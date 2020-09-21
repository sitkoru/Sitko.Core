using System;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Consul
{
    public interface IConsulModule
    {
    }

    public class ConsulModule<TConfig> : BaseApplicationModule<TConfig>, IConsulModule
        where TConfig : ConsulModuleConfig, new()
    {
        public ConsulModule(TConfig config, Application application) : base(config,
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
        }
    }

    public class ConsulModuleConfig
    {
        public string ConsulUri { get; set; } = "http://localhost:8500";
    }
}
