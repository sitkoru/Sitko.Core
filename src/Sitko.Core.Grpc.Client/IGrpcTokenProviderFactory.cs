using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Grpc.Client;

public interface IGrpcTokenProviderFactory<TClient> where TClient : ClientBase<TClient>
{
    public IGrpcTokenProvider GetProvider(IServiceProvider serviceProvider);
}

internal class GrpcTokenProviderFactory<TClient, TTokenProvider> : IGrpcTokenProviderFactory<TClient>
    where TClient : ClientBase<TClient>
    where TTokenProvider : class, IGrpcTokenProvider

{
    public IGrpcTokenProvider GetProvider(IServiceProvider serviceProvider) =>
        serviceProvider.GetServices<IGrpcTokenProvider>().OfType<TTokenProvider>().FirstOrDefault() ??
        throw new InvalidDataException($"Service {typeof(TTokenProvider)} is not register");
}
