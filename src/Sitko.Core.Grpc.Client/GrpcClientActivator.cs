using System.Diagnostics.CodeAnalysis;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Grpc.Client;

internal class GrpcClientActivator<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    TClient> where TClient : class
{
    private static readonly Func<ObjectFactory> CreateActivator = static () =>
        ActivatorUtilities.CreateFactory(typeof(TClient), new[] { typeof(CallInvoker) });

    private readonly IServiceProvider services;
    private ObjectFactory? activator;
    private bool initialized;
    private object? @lock;

    public GrpcClientActivator(IServiceProvider services) =>
        this.services = services ?? throw new ArgumentNullException(nameof(services));

    private ObjectFactory Activator
    {
        get
        {
            var currentActivator = LazyInitializer.EnsureInitialized(
                ref activator,
                ref initialized,
                ref @lock,
                CreateActivator);

            // TODO(JamesNK): Compiler thinks activator is nullable
            // Possibly remove in the future when compiler is fixed
            return currentActivator!;
        }
    }

    public TClient CreateClient(CallInvoker callInvoker) => (TClient)Activator(services, new object[] { callInvoker });
}
