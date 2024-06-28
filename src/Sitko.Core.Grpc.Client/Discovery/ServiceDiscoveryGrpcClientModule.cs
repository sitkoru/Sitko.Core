using Grpc.Core;

namespace Sitko.Core.Grpc.Client.Discovery;

public class ServiceDiscoveryGrpcClientModule<TClient> : GrpcClientModule<TClient,
    ServiceDiscoveryAddressResolver<TClient>,
    ServiceDiscoveryGrpcClientModuleOptions<TClient>>
    where TClient : ClientBase<TClient>
{
    public override string OptionsKey => $"Grpc:Client:{typeof(TClient).Name}";
}
