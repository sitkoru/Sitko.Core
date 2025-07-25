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
    protected override bool NeedSocketHandler => true;

    public override IEnumerable<Type> GetRequiredModules(IApplicationContext applicationContext,
        ServiceDiscoveryGrpcClientModuleOptions<TClient> options) =>
        [typeof(IServiceDiscoveryModule)];

    protected override Uri GenerateAddress(ServiceDiscoveryGrpcClientModuleOptions<TClient> options) => new(
        $"{ServiceDiscoveryResolverFactory.SchemeName}:///{GrpcServicesHelper.GetServiceNameForClient<TClient>()}");

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        ServiceDiscoveryGrpcClientModuleOptions<TClient> startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<ResolverFactory, ServiceDiscoveryResolverFactory>();
    }
}
