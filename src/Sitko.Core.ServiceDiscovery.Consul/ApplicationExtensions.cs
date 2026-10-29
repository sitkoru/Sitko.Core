using Microsoft.Extensions.Configuration;
using Sitko.Core.App;

namespace Sitko.Core.ServiceDiscovery.Consul
{
    public static class ApplicationExtensions
    {
        public static ISitkoCoreApplicationBuilder AddConsulServiceDiscovery(this ISitkoCoreApplicationBuilder builder,
            Action<ConsulServiceDiscoveryOptions>? configure = null) =>
            builder.AddServiceDiscovery().HasModule<ConsulServiceDiscoveryModule>()
                ? builder
                : builder.AddModule<ConsulServiceDiscoveryModule, ConsulServiceDiscoveryOptions>((context, options) =>
                {
                    configure?.Invoke(options);
                    options.Enabled &= context.Configuration.GetValue<bool?>("ServiceDiscovery:Enabled") != false;
                });
    }
}
