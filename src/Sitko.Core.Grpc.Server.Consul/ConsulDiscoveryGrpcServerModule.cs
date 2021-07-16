namespace Sitko.Core.Grpc.Server.Consul
{
    using Discovery;

    public class
        ConsulDiscoveryGrpcServerModule : DiscoveryGrpcServerModule<ConsulGrpcServicesRegistrar,
            ConsulDiscoveryGrpcServerModuleOptions>
    {
        public override string OptionsKey => "Grpc:Server:Consul";
    }

    public class ConsulDiscoveryGrpcServerModuleOptions : GrpcServerModuleOptions
    {}
}
