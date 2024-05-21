using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Grpc.Client;

internal interface IGrpcMetadataProviderFactory<TClient> where TClient : ClientBase<TClient>
{
    public IGrpcMetadataProvider GetProvider(IServiceProvider serviceProvider);
}

internal class GrpcMetadataProviderFactory<TClient, TMetadataProvider> : IGrpcMetadataProviderFactory<TClient>
    where TClient : ClientBase<TClient>
    where TMetadataProvider : class, IGrpcMetadataProvider

{
    public IGrpcMetadataProvider GetProvider(IServiceProvider serviceProvider) =>
        serviceProvider.GetServices<IGrpcMetadataProvider>().OfType<TMetadataProvider>().FirstOrDefault() ??
        throw new InvalidDataException($"Service {typeof(TMetadataProvider)} is not register");
}
