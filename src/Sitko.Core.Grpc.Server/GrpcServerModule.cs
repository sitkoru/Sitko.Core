namespace Sitko.Core.Grpc.Server
{
    public class GrpcServerModule : BaseGrpcServerModule<GrpcServerOptions>
    {
        public override string GetConfigKey()
        {
            return "Grpc:Server";
        }
    }
}
