using Grpc.Core;
using Grpc.Net.Client.Balancer;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.ServiceDiscovery;

namespace Sitko.Core.Grpc.Client.Discovery;

public class ServiceDiscoveryGrpcClientModule<TClient> : GrpcClientModule<TClient,
    ServiceDiscoveryGrpcClientModuleOptions<TClient>>
    where TClient : ClientBase<TClient>
{
    public override string OptionsKey => $"Grpc:Client:{typeof(TClient).Name}";

    protected override string ResolverFactoryScheme => ServiceDiscoveryResolverFactory.SchemeName;

    public override async Task InitAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider)
    {
        await base.InitAsync(applicationContext, serviceProvider);
        var resolver = serviceProvider.GetRequiredService<IServiceDiscoveryResolver>();
        await resolver.LoadAsync();
    }

    protected override ResolverFactory CreateResolverFactory(IServiceProvider sp) =>
        new ServiceDiscoveryResolverFactory();
}
