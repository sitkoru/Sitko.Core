using System.Collections.Concurrent;
using Grpc.Core;
using Grpc.Net.Client.Balancer;

namespace Sitko.Core.Grpc.Client.External;

internal static class ExternalGrpcClientModuleResolverFactory
{
    private static readonly ConcurrentDictionary<Guid, ExternalGrpcClientModuleResolver> Resolvers = new();

    public static ExternalGrpcClientModuleResolver GetOrCreate(Guid applicationId) =>
        Resolvers.GetOrAdd(applicationId, _ => new ExternalGrpcClientModuleResolver());
}

internal class ExternalGrpcClientModuleResolver
{
    private readonly ConcurrentDictionary<string, Uri> addresses = new();

    private readonly StaticResolverFactory staticResolverFactory;

    public ExternalGrpcClientModuleResolver() =>
        staticResolverFactory = new StaticResolverFactory(uri =>
        {
            if (addresses.TryGetValue(uri.LocalPath.TrimStart('/'), out var address))
            {
                return [new BalancerAddress(address.Host, address.Port)];
            }

            return [];
        });

    public ResolverFactory Factory => staticResolverFactory;

    public void Register<TClient>(Uri address) where TClient : ClientBase<TClient> =>
        addresses[GrpcServicesHelper.GetServiceNameForClient<TClient>()] = address;
}
