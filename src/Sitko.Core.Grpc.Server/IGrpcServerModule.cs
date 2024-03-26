namespace Sitko.Core.Grpc.Server;

public interface IGrpcServerModule
{
    void RegisterService<TService>(string? requiredAuthorizationSchemeName, bool enableGrpcWeb = false) where TService : class;
}

