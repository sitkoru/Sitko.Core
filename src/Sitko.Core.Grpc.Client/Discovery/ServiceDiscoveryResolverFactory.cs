using Grpc.Net.Client.Balancer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitko.Core.ServiceDiscovery;

namespace Sitko.Core.Grpc.Client.Discovery;

public class ServiceDiscoveryResolverFactory : ResolverFactory
{
    public const string SchemeName = "sd";

    public override string Name => SchemeName;

    public override Resolver Create(ResolverOptions options)
    {
        if (options.ChannelOptions.ServiceProvider is null)
        {
            throw new InvalidOperationException("Service provider is not configured");
        }

        var sdResolver = options.ChannelOptions.ServiceProvider.GetRequiredService<IServiceDiscoveryResolver>();
        return new ServiceDiscoveryResolver(sdResolver, options.Address,
            options.LoggerFactory.CreateLogger<ServiceDiscoveryResolver>());
    }
}

public class ServiceDiscoveryResolver(
    IServiceDiscoveryResolver resolver,
    Uri address,
    ILogger<ServiceDiscoveryResolver> logger)
    : Resolver
{
    public override void Start(Action<ResolverResult> listener) =>
        resolver.Subscribe(GrpcModuleConstants.GrpcServiceDiscoveryType,
            address.LocalPath.TrimStart('/'), services =>
            {
                var addresses = services.Select(s =>
                {
                    var balancerAddress = new BalancerAddress(s.Host, s.Port);
                    // fill attributes to avoid equality problems in BalancerAddressEqualityComparer
                    balancerAddress.Attributes.TryAdd("Name", s.Name);
                    return balancerAddress;
                }).ToArray();
                logger.LogInformation("Resolved {Address} service discovery to {Addresses}", address, addresses);
                listener(ResolverResult.ForResult(addresses));
            });
}
