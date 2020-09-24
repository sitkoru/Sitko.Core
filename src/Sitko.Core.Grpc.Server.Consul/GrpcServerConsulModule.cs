using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Grpc.Server.Discovery;

namespace Sitko.Core.Grpc.Server.Consul
{
    public class GrpcServerConsulModule : GrpcDiscoveryServerModule<ConsulGrpcServicesRegistrar, GrpcServerConsulModuleConfig>
    {
        public GrpcServerConsulModule(GrpcServerConsulModuleConfig config, Application application) : base(config,
            application)
        {
        }
    }

    public class GrpcServerConsulModuleConfig : GrpcServerOptions
    {
    }
}
