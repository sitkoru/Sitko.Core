using System.Text.Json.Serialization;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Client;

public class GrpcClientModuleOptions : BaseModuleOptions
{
    public Uri? Address { get; set; }
    public int? MaxReceiveMessageSize { get; set; } = new GrpcChannelOptions().MaxReceiveMessageSize;
    public int? MaxSendMessageSize { get; set; }
    public bool EnableHttp2UnencryptedSupport { get; set; }
    public bool DisableCertificatesValidation { get; set; }
    public TimeSpan? DefaultDeadline { get; set; } = TimeSpan.FromMinutes(30);
    [JsonIgnore] public Action<GrpcChannelOptions>? ConfigureChannelOptions { get; set; }
    [JsonIgnore] public Func<HttpMessageHandler, HttpMessageHandler>? ConfigureHttpHandler { get; set; }
    public bool UseGrpcWeb { get; set; }
    public TimeSpan? PooledConnectionLifetime { get; set; } = TimeSpan.FromMinutes(2);
    public LoadBalancingPolicy LoadBalancingPolicy { get; set; } = LoadBalancingPolicy.RoundRobin;
    [JsonIgnore] public RetryPolicy? RetryPolicy { get; set; } = GrpcClientRetryPolicies.Default;
    private const string AuthorizationHeader = "Authorization";
    private protected List<Action<IServiceCollection>> ConfigureServicesActions { get; } = [];
    private Func<IServiceProvider, IGrpcTokenProvider>? resolveTokenProvider;
    private Func<IServiceProvider, IGrpcMetadataProvider>? resolveMetadataProvider;
    private readonly Dictionary<Type, Action<IServiceCollection, IHttpClientBuilder>> interceptorActions = [];

    private protected void ConfigureClient<TClient>(IServiceCollection services, IHttpClientBuilder builder)
        where TClient : ClientBase<TClient>
    {
        foreach (var (_, configure) in interceptorActions)
        {
            configure(services, builder);
        }


        foreach (var configure in ConfigureServicesActions)
        {
            configure(services);
        }

        _ = builder.AddCallCredentials(async (_, metadata, serviceProvider) =>
        {
            var tokenProviderFactory = serviceProvider.GetService<IGrpcTokenProviderFactory<TClient>>();
            var tokenProvider = tokenProviderFactory?.GetProvider(serviceProvider) ?? resolveTokenProvider?.Invoke(serviceProvider);
            if (tokenProvider is not null)
            {
                var token = await tokenProvider.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    SetMetadata(metadata, AuthorizationHeader, token);
                }
            }


            var metadataProviderFactory = serviceProvider.GetService<IGrpcMetadataProviderFactory<TClient>>();
            var metadataProvider = metadataProviderFactory?.GetProvider(serviceProvider) ?? resolveMetadataProvider?.Invoke(serviceProvider);
            if (metadataProvider is not null)
            {
                var newMetadata = await metadataProvider.GetMetadataAsync();
                if (newMetadata?.Count > 0)
                {
                    foreach (var (key, value) in newMetadata)
                    {
                        SetMetadata(metadata, key, value);
                    }
                }
            }
        });
    }

    private static void SetMetadata(Metadata metadata, string key, string value)
    {
        var oldEntries = metadata.GetAll(key);
        foreach (var entry in oldEntries)
        {
            _ = metadata.Remove(entry);
        }

        metadata.Add(key, value);
    }

    public GrpcClientModuleOptions AddMetadataProvider<TMetadataProvider>()
        where TMetadataProvider : class, IGrpcMetadataProvider
    {
        ConfigureServicesActions.Add(static services => services.TryAddTransient<TMetadataProvider>());
        resolveMetadataProvider = static provider => provider.GetRequiredService<TMetadataProvider>();
        return this;
    }

    public GrpcClientModuleOptions AddTokenAuth<TTokenProvider>() where TTokenProvider : class, IGrpcTokenProvider
    {
        ConfigureServicesActions.Add(static services => services.TryAddTransient<TTokenProvider>());
        resolveTokenProvider = static provider => provider.GetRequiredService<TTokenProvider>();
        return this;
    }

    public GrpcClientModuleOptions AddInterceptor<TInterceptor>() where TInterceptor : Interceptor
    {
        interceptorActions[typeof(TInterceptor)] = static (services, builder) =>
        {
            services.TryAddSingleton<TInterceptor>();
            _ = builder.AddInterceptor<TInterceptor>();
        };
        return this;
    }
}

public class GrpcClientModuleOptions<TClient> : GrpcClientModuleOptions where TClient : ClientBase<TClient>
{
    internal void ConfigureClient(IServiceCollection services, IHttpClientBuilder builder) =>
        ConfigureClient<TClient>(services, builder);

    public new GrpcClientModuleOptions<TClient> AddMetadataProvider<TMetadataProvider>()
        where TMetadataProvider : class, IGrpcMetadataProvider
    {
        ConfigureServicesActions.Add(static services =>
        {
            _ = services
                .AddTransient<IGrpcMetadataProviderFactory<TClient>,
                    GrpcMetadataProviderFactory<TClient, TMetadataProvider>>();
            services.TryAddTransient<TMetadataProvider>();
        });
        return this;
    }

    public new GrpcClientModuleOptions<TClient> AddTokenAuth<TTokenProvider>()
        where TTokenProvider : class, IGrpcTokenProvider
    {
        ConfigureServicesActions.Add(static services =>
        {
            _ = services
                .AddTransient<IGrpcTokenProviderFactory<TClient>, GrpcTokenProviderFactory<TClient, TTokenProvider>>();
            services.TryAddTransient<TTokenProvider>();
        });
        return this;
    }

    public new GrpcClientModuleOptions<TClient> AddInterceptor<TInterceptor>() where TInterceptor : Interceptor
    {
        _ = base.AddInterceptor<TInterceptor>();
        return this;
    }
}
