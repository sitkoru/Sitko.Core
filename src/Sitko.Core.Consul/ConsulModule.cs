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
        where TConfig : ConsulModuleOptions, new()
    {
        public override string GetOptionsKey()
        {
            return "Consul";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TConfig startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddSingleton<IConsulClient, ConsulClient>(serviceProvider =>
            {
                var options = GetOptions(serviceProvider);
                return new ConsulClient(config => { config.Address = new Uri(options.ConsulUri); });
            });
        }
    }

    public class ConsulModuleOptions : BaseModuleOptions
    {
        public string ConsulUri { get; set; } = "http://localhost:8500";
    }
}
