namespace Sitko.Core.Grpc.Client
{
    using System;
    using Discovery;
    using global::Grpc.Core;
    using global::Grpc.Net.ClientFactory;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    public interface IGrpcClientProvider<out TClient> where TClient : ClientBase<TClient>
    {
        TClient Instance { get; }
        Uri? CurrentAddress { get; }
    }

    public class GrpcClientProvider<TClient> : IGrpcClientProvider<TClient>
        where TClient : ClientBase<TClient>
    {
        private readonly IOptionsMonitor<GrpcClientFactoryOptions> clientFactoryOptionsMonitor;
        private readonly IGrpcServiceAddressResolver<TClient> resolver;
        private readonly IServiceProvider serviceProvider;

        public GrpcClientProvider(IServiceProvider serviceProvider,
            IOptionsMonitor<GrpcClientFactoryOptions> clientFactoryOptionsMonitor,
            IGrpcServiceAddressResolver<TClient> resolver)
        {
            this.serviceProvider = serviceProvider;
            this.clientFactoryOptionsMonitor = clientFactoryOptionsMonitor;
            this.resolver = resolver;
            this.resolver.OnChange += (_, _) => OnChange();
            CurrentAddress = this.resolver.GetAddress();
        }

        public TClient Instance => serviceProvider.GetRequiredService<TClient>();
        public Uri? CurrentAddress { get; private set; }

        private void OnChange()
        {
            var options = clientFactoryOptionsMonitor.Get(typeof(TClient).Name);
            CurrentAddress = options.Address = resolver.GetAddress();
        }
    }
}
