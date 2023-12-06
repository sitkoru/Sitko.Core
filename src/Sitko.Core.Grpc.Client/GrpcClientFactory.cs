using System.Diagnostics.CodeAnalysis;
using Grpc.Core.Interceptors;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Grpc.Client;

internal class GrpcClientFactory : global::Grpc.Net.ClientFactory.GrpcClientFactory
{
    private readonly GrpcCallInvokerFactory callInvokerFactory;
    private readonly IOptionsMonitor<GrpcClientFactoryOptions> grpcClientFactoryOptionsMonitor;
    private readonly IServiceProvider serviceProvider;

    public GrpcClientFactory(IServiceProvider serviceProvider, GrpcCallInvokerFactory callInvokerFactory,
        IOptionsMonitor<GrpcClientFactoryOptions> grpcClientFactoryOptionsMonitor)
    {
        this.serviceProvider = serviceProvider;
        this.callInvokerFactory = callInvokerFactory;
        this.grpcClientFactoryOptionsMonitor = grpcClientFactoryOptionsMonitor;
    }

    public override TClient CreateClient<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TClient>(string name) where TClient : class
    {
        var defaultClientActivator = serviceProvider.GetService<GrpcClientActivator<TClient>>();
        if (defaultClientActivator == null)
        {
            throw new InvalidOperationException($"No gRPC client configured with name '{name}'.");
        }

        var callInvoker = callInvokerFactory.CreateInvoker(name, typeof(TClient));

        var clientFactoryOptions = grpcClientFactoryOptionsMonitor.Get(name);

        var resolvedCallInvoker = GrpcFactoryHelpers.BuildInterceptors(
            callInvoker,
            serviceProvider,
            clientFactoryOptions,
            InterceptorScope.Client);

#pragma warning disable CS0618 // Type or member is obsolete
        if (clientFactoryOptions.Interceptors.Count != 0)
        {
            resolvedCallInvoker = resolvedCallInvoker.Intercept(clientFactoryOptions.Interceptors.ToArray());
        }
#pragma warning restore CS0618 // Type or member is obsolete

        if (clientFactoryOptions.CallOptionsActions.Count != 0)
        {
            resolvedCallInvoker = new CallOptionsConfigurationInvoker(resolvedCallInvoker,
                clientFactoryOptions.CallOptionsActions, serviceProvider);
        }

        if (clientFactoryOptions.Creator != null)
        {
            var c = clientFactoryOptions.Creator(resolvedCallInvoker);
            if (c is TClient client)
            {
                return client;
            }

            if (c == null)
            {
                throw new InvalidOperationException("A null instance was returned by the configured client creator.");
            }

            throw new InvalidOperationException(
                $"The {c.GetType().FullName} instance returned by the configured client creator is not compatible with {typeof(TClient).FullName}.");
        }

        return defaultClientActivator.CreateClient(resolvedCallInvoker);
    }
}
