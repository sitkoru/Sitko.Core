using Grpc.AspNetCore.Server;

namespace Sitko.Core.Grpc.Server;

public interface IGrpcServerModule
{
    public void RegisterService<TService>(string? requiredAuthorizationSchemeName, bool enableGrpcWeb = false,
        Action<GrpcServiceOptions>? configure = null) where TService : class;
}

