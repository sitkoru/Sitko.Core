using Grpc.AspNetCore.Server;

namespace Sitko.Core.Grpc.Server
{
    public sealed class GrpcServerBuilder(GrpcServerModule module)
    {
        public GrpcServerBuilder RegisterService<TService>(Action<GrpcServiceOptions>? configure = null) where TService : class
        {
            module.AddService<TService>(configure);
            return this;
        }

        public GrpcServerBuilder RegisterServiceWithGrpcWeb<TService>(Action<GrpcServiceOptions>? configure = null)
            where TService : class
        {
            module.AddServiceWithGrpcWeb<TService>(configure);
            return this;
        }
    }
}
