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
            var tokenProvider = serviceProvider.GetService<IGrpcTokenProvider>();
            if (tokenProvider is not null)
            {
                var token = await tokenProvider.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    var oldAuthHeaders = metadata.GetAll(AuthorizationHeader);
                    foreach (var entry in oldAuthHeaders)
                    {
                        metadata.Remove(entry);
                    }

                    metadata.Add(AuthorizationHeader, token);
                }
            }

            var metadataProviders = serviceProvider.GetServices<IGrpcMetadataProvider>();
            foreach (var metadataProvider in metadataProviders)
            {
                var newMetadata = await metadataProvider.GetMetadataAsync();
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
            }
        });
    }

    public GrpcClientModuleOptions<TClient> AddMetadataProvider<TMetadataProvider>()
        where TMetadataProvider : class, IGrpcMetadataProvider
    {
        configureServicesActions.Add(services =>
        {
            services.AddTransient<IGrpcMetadataProvider, IGrpcMetadataProvider>();
        });
        return this;
    }

    public GrpcClientModuleOptions<TClient> AddTokenAuth<TTokenProvider>()
        where TTokenProvider : class, IGrpcTokenProvider
    {
        configureServicesActions.Add(services =>
        {
            services.TryAddTransient<IGrpcTokenProvider, TTokenProvider>();
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
