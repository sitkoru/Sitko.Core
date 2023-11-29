using System.Collections.Concurrent;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client;

internal readonly record struct EntryKey(string Name, Type Type);

internal class GrpcCallInvokerFactory
{
    private readonly ConcurrentDictionary<EntryKey, CallInvoker> activeChannels = new();
    private readonly IOptionsMonitor<GrpcClientFactoryOptions> grpcClientFactoryOptionsMonitor;
    private readonly IOptionsMonitor<HttpClientFactoryOptions> httpClientFactoryOptionsMonitor;
    private readonly Func<EntryKey, CallInvoker> invokerFactory;
    private readonly ILoggerFactory loggerFactory;
    private readonly IHttpMessageHandlerFactory messageHandlerFactory;
    private readonly ConcurrentDictionary<EntryKey, IGrpcServiceAddressResolver> resolvers = new();

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<GrpcCallInvokerFactory> logger;

    public GrpcCallInvokerFactory(
        IServiceScopeFactory scopeFactory,
        ILoggerFactory loggerFactory,
        IOptionsMonitor<GrpcClientFactoryOptions> grpcClientFactoryOptionsMonitor,
        IOptionsMonitor<HttpClientFactoryOptions> httpClientFactoryOptionsMonitor,
        IHttpMessageHandlerFactory messageHandlerFactory)
    {
        this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        logger = loggerFactory.CreateLogger<GrpcCallInvokerFactory>();
        this.grpcClientFactoryOptionsMonitor = grpcClientFactoryOptionsMonitor;
        this.httpClientFactoryOptionsMonitor = httpClientFactoryOptionsMonitor;
        this.messageHandlerFactory = messageHandlerFactory;

        this.scopeFactory = scopeFactory;
        invokerFactory = CreateInvoker;
    }

    public CallInvoker CreateInvoker(string name, Type type) =>
        activeChannels.GetOrAdd(new EntryKey(name, type), invokerFactory);

    private IGrpcServiceAddressResolver GetResolver(IServiceProvider serviceProvider, EntryKey key) =>
        resolvers.GetOrAdd(key, entryKey =>
        {
            var resolverType = typeof(IGrpcServiceAddressResolver<>).MakeGenericType(entryKey.Type);
            if (serviceProvider.GetRequiredService(resolverType) is not IGrpcServiceAddressResolver resolver)
            {
                throw new InvalidOperationException($"Can't get address resolver for {entryKey.Type}");
            }

            resolver.OnChange += (_, _) =>
            {
                activeChannels.TryRemove(entryKey, out _);
            };
            return resolver;
        });

    private CallInvoker CreateInvoker(EntryKey key)
    {
        var (name, _) = (key.Name, key.Type);
        var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var httpClientFactoryOptions = httpClientFactoryOptionsMonitor.Get(name);
            if (httpClientFactoryOptions.HttpClientActions.Count > 0)
            {
                throw new InvalidOperationException(
                    $"The ConfigureHttpClient method is not supported when creating gRPC clients. Unable to create client with name '{name}'.");
            }


            var clientFactoryOptions = grpcClientFactoryOptionsMonitor.Get(name);
            var httpHandler = messageHandlerFactory.CreateHandler(name);

            if (httpHandler == null)
            {
#pragma warning disable CA2208
                throw new ArgumentNullException(nameof(httpHandler));
#pragma warning restore CA2208
            }

            var channelOptions = new GrpcChannelOptions
            {
                HttpHandler = httpHandler, LoggerFactory = loggerFactory, ServiceProvider = services
            };

            if (clientFactoryOptions.ChannelOptionsActions.Count > 0)
            {
                foreach (var applyOptions in clientFactoryOptions.ChannelOptionsActions)
                {
                    applyOptions(channelOptions);
                }
            }

            var resolver = GetResolver(services, key);
            var address = resolver.GetAddress();
            if (address == null)
            {
                logger.LogError("Could not resolve the address for gRPC client '{Name}'", name);
                address = new Uri("https://localhost"); // fake address
            }

            var channel = GrpcChannel.ForAddress(address, channelOptions);

            var httpClientCallInvoker = channel.CreateCallInvoker();

            var resolvedCallInvoker = GrpcFactoryHelpers.BuildInterceptors(
                httpClientCallInvoker,
                services,
                clientFactoryOptions,
                InterceptorScope.Channel);

            return resolvedCallInvoker;
        }
        catch
        {
            // If something fails while creating the handler, dispose the services.
            scope.Dispose();
            throw;
        }
    }
}
