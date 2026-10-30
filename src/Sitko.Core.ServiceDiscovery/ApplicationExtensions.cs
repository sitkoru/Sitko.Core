using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.ServiceDiscovery;

public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddServiceDiscovery(this IHostApplicationBuilder builder,
        Action<ServiceDiscoveryModuleOptions>? configure = null)
    {
        _ = builder.GetSitkoCore().AddServiceDiscovery(configure);
        return builder;
    }

    public static ISitkoCoreApplicationBuilder AddServiceDiscovery(this ISitkoCoreApplicationBuilder builder,
        Action<ServiceDiscoveryModuleOptions>? configure = null) =>
        builder.HasModule<ServiceDiscoveryModule>()
            ? builder
            : builder.AddModule<ServiceDiscoveryModule, ServiceDiscoveryModuleOptions>(configure);
}
