using System;
using Consul;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Consul
{
    public interface IConsulModule
    {
    }

    public class ConsulModule<TConfig> : BaseApplicationModule<TConfig>, IConsulModule
        where TConfig : ConsulModuleConfig, new()
    {
        public override string GetConfigKey()
        {
            return "Consul";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TConfig startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddSingleton<IConsulClient, ConsulClient>(serviceProvider =>
            {
                var options = GetConfig(serviceProvider);
                return new ConsulClient(config => { config.Address = new Uri(options.ConsulUri); });
            });
        }
    }

    public class ConsulModuleConfig : BaseModuleConfig
    {
        public string ConsulUri { get; set; } = "http://localhost:8500";
    }
}
