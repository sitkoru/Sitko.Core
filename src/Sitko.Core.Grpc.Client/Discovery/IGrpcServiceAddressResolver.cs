namespace Sitko.Core.Grpc.Client.Discovery
{
    using System;
    using System.Threading.Tasks;
    using global::Grpc.Core;

    public interface IGrpcServiceAddressResolver<TClient> where TClient : ClientBase<TClient>
    {
        public Task InitAsync();
        public Uri? GetAddress();
        event EventHandler? OnChange;
    }
}
