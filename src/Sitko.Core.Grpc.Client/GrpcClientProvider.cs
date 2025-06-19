using Grpc.Core;

namespace Sitko.Core.Grpc.Client;

public interface IGrpcClientProvider<out TClient> where TClient : ClientBase<TClient>
{
    TClient Instance { get; }
}

public class GrpcClientProvider<TClient>(TClient client) : IGrpcClientProvider<TClient>
    where TClient : ClientBase<TClient>
{
    public TClient Instance { get; } = client;
}
