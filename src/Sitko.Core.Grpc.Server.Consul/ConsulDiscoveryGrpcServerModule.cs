using Sitko.Core.Grpc.Server.Discovery;

namespace Sitko.Core.Grpc.Server.Consul;

public class
    ConsulDiscoveryGrpcServerModule : DiscoveryGrpcServerModule<ConsulGrpcServicesRegistrar,
        ConsulDiscoveryGrpcServerModuleOptions>
{
    public override string OptionsKey => "Grpc:Server:Consul";
}

public class ConsulDiscoveryGrpcServerModuleOptions : GrpcServerModuleOptions
{
}

