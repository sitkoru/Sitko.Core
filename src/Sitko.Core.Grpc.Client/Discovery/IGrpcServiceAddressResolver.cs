using Grpc.Core;

namespace Sitko.Core.Grpc.Client.Discovery;

public interface IGrpcServiceAddressResolver
{
    public Task InitAsync();
    public Uri? GetAddress();
    event EventHandler? OnChange;
}

public interface IGrpcServiceAddressResolver<TClient> : IGrpcServiceAddressResolver where TClient : ClientBase<TClient>;
