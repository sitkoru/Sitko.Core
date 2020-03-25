using System;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Grpc.Client
{
    public interface IGrpcClientProvider<TClient> where TClient : ClientBase<TClient>
    {
        TClient Instance { get; }
    }

    public class GrpcClientProvider<TClient> : IGrpcClientProvider<TClient> where TClient : ClientBase<TClient>
    {
        private readonly IServiceProvider _serviceProvider;

        public GrpcClientProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        public TClient Instance => _serviceProvider.GetService<TClient>();
    }
}
