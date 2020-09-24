using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server
{
    public class GrpcServerModule : BaseGrpcServerModule<GrpcServerOptions>
    {
        public GrpcServerModule(GrpcServerOptions config, Application application) : base(config,
            application)
        {
        }
    }
}
