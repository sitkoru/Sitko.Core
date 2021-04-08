using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Sitko.Core.Grpc.Client.Discovery
{
    public interface IGrpcServiceAddressResolver<TClient> where TClient : ClientBase<TClient>
    {
        public Task InitAsync();
        public Uri? GetAddress();
        event EventHandler? OnChange;
    }
}
