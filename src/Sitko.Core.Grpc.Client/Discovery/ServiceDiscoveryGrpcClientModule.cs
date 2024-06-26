using Grpc.Core;
using Sitko.Core.Grpc.Client.Discovery;
using Sitko.Core.ServiceDiscovery;

namespace Sitko.Core.Grpc.Client;

public class ServiceDiscoveryGrpcClientModule<TClient> : GrpcClientModule<TClient,
    ServiceDiscoveryAddressResolver<TClient>,
    ServiceDiscoveryGrpcClientModuleOptions<TClient>>
    where TClient : ClientBase<TClient>
{
    public override string OptionsKey => $"Grpc:Client:{typeof(TClient).Name}";
}

public class ServiceDiscoveryAddressResolver<TClient>(IServiceDiscoveryProvider provider)
    : IGrpcServiceAddressResolver<TClient>
    where TClient : ClientBase<TClient>
{
    public async Task InitAsync() => await provider.LoadAsync();

    public Uri? GetAddress()
    {
        var service = provider.Resolve(GrpcModuleConstants.GrpcServiceDiscoveryType,
            GrpcServicesHelper.GetServiceNameForClient<TClient>());
        return service is not null ? new Uri($"{service.Scheme}://{service.Host}:{service.Port}") : null;
    }

    public event EventHandler? OnChange;
}

public class ServiceDiscoveryGrpcClientModuleOptions<TClient> : GrpcClientModuleOptions<TClient>
    where TClient : ClientBase<TClient>
{
}
