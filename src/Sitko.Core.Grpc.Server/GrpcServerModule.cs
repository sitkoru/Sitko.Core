namespace Sitko.Core.Grpc.Server
{
    public class GrpcServerModule : BaseGrpcServerModule<GrpcServerModuleOptions>
    {
        public override string GetOptionsKey()
        {
            return "Grpc:Server";
        }
    }
}
