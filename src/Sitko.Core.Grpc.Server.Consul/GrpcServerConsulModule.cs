using Sitko.Core.Grpc.Server.Discovery;

namespace Sitko.Core.Grpc.Server.Consul
{
    public class
        GrpcServerConsulModule : GrpcDiscoveryServerModule<ConsulGrpcServicesRegistrar, GrpcServerConsulModuleConfig>
    {
        public override string GetConfigKey()
        {
            return "Grpc:Server:Consul";
        }
    }

    public class GrpcServerConsulModuleConfig : GrpcServerOptions
    {
    }
}
