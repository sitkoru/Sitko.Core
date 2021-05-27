namespace Sitko.Core.Grpc.Server
{
    public class GrpcServerModule : BaseGrpcServerModule<GrpcServerOptions>
    {
        public override string GetOptionsKey()
        {
            return "Grpc:Server";
        }
    }
}
