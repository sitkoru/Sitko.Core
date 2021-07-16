namespace Sitko.Core.Grpc.Client.External
{
    using System;
    using System.Threading.Tasks;
    using Discovery;
    using global::Grpc.Core;

    public class ExternalGrpcServiceAddressResolver<TClient> : IGrpcServiceAddressResolver<TClient>
        where TClient : ClientBase<TClient>
    {
        private readonly Uri address;

        public ExternalGrpcServiceAddressResolver(Uri address) => this.address = address;

        public ExternalGrpcServiceAddressResolver(string address) : this(new Uri(address))
        {
        }

        public Task InitAsync() => Task.CompletedTask;

        public Uri GetAddress() => address;

        public event EventHandler? OnChange;
    }
}
