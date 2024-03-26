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
    private readonly Dictionary<Type, Action<IServiceCollection, IHttpClientBuilder>> interceptorActions = new();
    private Action<IServiceCollection, IHttpClientBuilder>? configureAuthAction;
    public bool EnableHttp2UnencryptedSupport { get; set; }
    public bool DisableCertificatesValidation { get; set; }
    [JsonIgnore] public Action<GrpcChannelOptions>? ConfigureChannelOptions { get; set; }
    [JsonIgnore] public Func<HttpClientHandler, HttpMessageHandler>? ConfigureHttpHandler { get; set; }

    internal void ConfigureClient(IServiceCollection services, IHttpClientBuilder builder)
    {
        foreach (var (_, configure) in interceptorActions)
        {
            configure(services, builder);
        }

        configureAuthAction?.Invoke(services, builder);
    }

    public GrpcClientModuleOptions<TClient> AddMetadataProvider<TMetadataProvider>()
        where TMetadataProvider : class, IGrpcMetadataProvider
    {
        configureAuthAction = (services, builder) =>
        {
            services.TryAddTransient<IGrpcMetadataProvider, TMetadataProvider>();
            builder.AddCallCredentials(async (_, metadata, serviceProvider) =>
            {
                var provider = serviceProvider.GetRequiredService<IGrpcMetadataProvider>();
                var newMetadata = await provider.GetMetadataAsync();
                if (newMetadata?.Count > 0)
                {
                    foreach (var (key, value) in newMetadata)
                    {
                        var oldEntries = metadata.GetAll(key);
                        foreach (var entry in oldEntries)
                        {
                            metadata.Remove(entry);
                        }

                        metadata.Add(key, value);
                    }
                }
            });
        };
        return this;
    }

    public GrpcClientModuleOptions<TClient> AddTokenAuth<TTokenProvider>()
        where TTokenProvider : class, IGrpcTokenProvider
    {
        const string AuthorizationHeader = "Authorization";
        configureAuthAction = (services, builder) =>
        {
            services.TryAddTransient<IGrpcTokenProvider, TTokenProvider>();
            builder.AddCallCredentials(async (_, metadata, serviceProvider) =>
            {
                var provider = serviceProvider.GetRequiredService<IGrpcTokenProvider>();
                var token = await provider.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    var oldAuthHeaders = metadata.GetAll(AuthorizationHeader);
                    foreach (var entry in oldAuthHeaders)
                    {
                        metadata.Remove(entry);
                    }

                    metadata.Add(AuthorizationHeader, token);
                }
            });
        };
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
