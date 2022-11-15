using Grpc.Core;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client.External;

public class ExternalGrpcServiceAddressResolver<TClient> : IGrpcServiceAddressResolver<TClient>
    where TClient : ClientBase<TClient>
{
    private readonly Uri address;

    public ExternalGrpcServiceAddressResolver(Uri address) => this.address = address;

    public ExternalGrpcServiceAddressResolver(string address) : this(new Uri(address))
    {
    }

    public Task InitAsync()
    {
        OnChange?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Uri GetAddress() => address;
    public event EventHandler? OnChange;
}

