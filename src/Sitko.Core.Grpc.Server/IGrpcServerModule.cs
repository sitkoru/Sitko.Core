namespace Sitko.Core.Grpc.Server;

public interface IGrpcServerModule
{
    void RegisterService<TService>() where TService : class;
}

