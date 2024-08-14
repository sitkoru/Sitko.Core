using System.Text.Json.Serialization;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Client;

[PublicAPI]
public class GrpcClientModuleOptions<TClient> : BaseModuleOptions where TClient : ClientBase<TClient>
{
    private const string AuthorizationHeader = "Authorization";
    private readonly Dictionary<Type, Action<IServiceCollection, IHttpClientBuilder>> interceptorActions = new();
    private readonly List<Action<IServiceCollection>> configureServicesActions = new();
    public bool EnableHttp2UnencryptedSupport { get; set; }
    public bool DisableCertificatesValidation { get; set; }
    public TimeSpan? DefaultDeadline { get; set; } = TimeSpan.FromMinutes(30);
    [JsonIgnore] public Action<GrpcChannelOptions>? ConfigureChannelOptions { get; set; }
    [JsonIgnore] public Func<HttpClientHandler, HttpMessageHandler>? ConfigureHttpHandler { get; set; }

    internal void ConfigureClient(IServiceCollection services, IHttpClientBuilder builder)
    {
        foreach (var (_, configure) in interceptorActions)
        {
            configure(services, builder);
        }


        foreach (var configure in configureServicesActions)
        {
            configure(services);
        }

        builder.AddCallCredentials(async (_, metadata, serviceProvider) =>
        {
            var tokenProviderFactory = serviceProvider.GetService<IGrpcTokenProviderFactory<TClient>>();
            if (tokenProviderFactory is not null)
            {
                var tokenProvider = tokenProviderFactory.GetProvider(serviceProvider);
                var token = await tokenProvider.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    SetMetadata(metadata, AuthorizationHeader, token);
                }
            }


            var metadataProviderFactory = serviceProvider.GetService<IGrpcMetadataProviderFactory<TClient>>();
            if (metadataProviderFactory is not null)
            {
                var metadataProvider = metadataProviderFactory.GetProvider(serviceProvider);
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
            metadata.Remove(entry);
        }

        metadata.Add(key, value);
    }

    public GrpcClientModuleOptions<TClient> AddMetadataProvider<TMetadataProvider>()
        where TMetadataProvider : class, IGrpcMetadataProvider
    {
        configureServicesActions.Add(services =>
        {
            services.AddTransient<IGrpcMetadataProviderFactory<TClient>, GrpcMetadataProviderFactory<TClient, TMetadataProvider>>();
            services.AddTransient<IGrpcMetadataProvider, TMetadataProvider>();
        });
        return this;
    }

    public GrpcClientModuleOptions<TClient> AddTokenAuth<TTokenProvider>()
        where TTokenProvider : class, IGrpcTokenProvider
    {
        configureServicesActions.Add(services =>
        {
            services.AddTransient<IGrpcTokenProviderFactory<TClient>, GrpcTokenProviderFactory<TClient, TTokenProvider>>();
            services.AddTransient<IGrpcTokenProvider, TTokenProvider>();
        });
        return this;
    }

    public GrpcClientModuleOptions<TClient> AddInterceptor<TInterceptor>() where TInterceptor : Interceptor
    {
        interceptorActions[typeof(TInterceptor)] = (services, builder) =>
        {
            services.TryAddSingleton<TInterceptor>();
            builder.AddInterceptor<TInterceptor>();
        };
        return this;
    }
}
