using Grpc.Core;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client;

public interface IGrpcClientProvider<out TClient> where TClient : ClientBase<TClient>
{
    TClient Instance { get; }
    Uri? CurrentAddress { get; }
}

public class GrpcClientProvider<TClient> : IGrpcClientProvider<TClient>
    where TClient : ClientBase<TClient>
{
    private readonly IGrpcServiceAddressResolver<TClient> resolver;

    public GrpcClientProvider(TClient client, IGrpcServiceAddressResolver<TClient> resolver)
    {
        Instance = client;
        this.resolver = resolver;
    }

    public TClient Instance { get; }

    public Uri? CurrentAddress => resolver.GetAddress();
}
