using Grpc.Core;

namespace Sitko.Core.Grpc.Client.Discovery;

public class ServiceDiscoveryGrpcClientModuleOptions<TClient> : GrpcClientModuleOptions<TClient>
    where TClient : ClientBase<TClient>
{
}