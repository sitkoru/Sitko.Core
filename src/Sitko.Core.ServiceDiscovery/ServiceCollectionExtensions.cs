using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.ServiceDiscovery;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddToServiceDiscovery(this IServiceCollection services,
        ServiceDiscoveryService service)
    {
        services.Configure<ServiceDiscoveryOptions>(options =>
        {
            options.RegisterService(service);
        });
        return services;
    }
}