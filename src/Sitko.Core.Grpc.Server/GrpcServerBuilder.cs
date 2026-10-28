namespace Sitko.Core.Grpc.Server
{
    public sealed class GrpcServerBuilder(GrpcServerModule module)
    {
        public GrpcServerBuilder RegisterService<TService>() where TService : class
        {
            module.AddService<TService>();
            return this;
        }
    }
}
