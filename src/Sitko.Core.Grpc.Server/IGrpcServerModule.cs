namespace Sitko.Core.Grpc.Server;

public interface IGrpcServerModule
{
    void RegisterService<TService>(string? requiredAuthorizarionSchemeName) where TService : class;
}

