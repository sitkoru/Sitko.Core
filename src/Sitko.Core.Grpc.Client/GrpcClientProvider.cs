using System;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client
{
    public interface IGrpcClientProvider<TClient> where TClient : ClientBase<TClient>
    {
        TClient Instance { get; }
        Uri? CurrentAddress { get; }
    }

    public class GrpcClientProvider<TClient> : IGrpcClientProvider<TClient>
        where TClient : ClientBase<TClient>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptionsMonitor<GrpcClientFactoryOptions> _clientFactoryOptionsMonitor;
        private readonly IGrpcServiceAddressResolver<TClient> _resolver;

        public GrpcClientProvider(IServiceProvider serviceProvider,
            IOptionsMonitor<GrpcClientFactoryOptions> clientFactoryOptionsMonitor,
            IGrpcServiceAddressResolver<TClient> resolver)
        {
            _serviceProvider = serviceProvider;
            _clientFactoryOptionsMonitor = clientFactoryOptionsMonitor;
            _resolver = resolver;
            _resolver.OnChange += (_, _) => OnChange();
            CurrentAddress = _resolver.GetAddress();
        }

        private void OnChange()
        {
            var options = _clientFactoryOptionsMonitor.Get(typeof(TClient).Name);
            CurrentAddress = options.Address = _resolver.GetAddress();
        }

        public TClient Instance => _serviceProvider.GetRequiredService<TClient>();
        public Uri? CurrentAddress { get; private set; }
    }
}
