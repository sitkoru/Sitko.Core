using System;
using System.Threading.Tasks;
using Grpc.Core;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client.External
{
    public class ExternalGrpcServiceAddressResolver<TClient> : IGrpcServiceAddressResolver<TClient>
        where TClient : ClientBase<TClient>
    {
        private readonly Uri _address;

        public ExternalGrpcServiceAddressResolver(Uri address)
        {
            _address = address;
        }

        public ExternalGrpcServiceAddressResolver(string address) : this(new Uri(address))
        {
        }

        public Task InitAsync()
        {
            return Task.CompletedTask;
        }

        public Uri GetAddress()
        {
            return _address;
        }

        public event EventHandler? OnChange;
    }
}
