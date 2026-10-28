using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.ServiceDiscovery;
using Sitko.Core.ServiceDiscovery;
using Grpc.Net.Client.Configuration;
using Grpc.Net.Client.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Sitko.Core.App;
using Sitko.Core.App.OpenTelemetry;

namespace Sitko.Core.Grpc.Client;

public interface IGrpcClientModule<TClient> where TClient : ClientBase<TClient>;

public abstract class GrpcClientModule<TClient, TGrpcClientModuleOptions> :
    BaseApplicationModule<TGrpcClientModuleOptions>,
    IGrpcClientModule<TClient>, IOpenTelemetryModule<TGrpcClientModuleOptions>
    where TClient : ClientBase<TClient>
    where TGrpcClientModuleOptions : GrpcClientModuleOptions<TClient>, new()
{
    protected virtual bool NeedSocketHandler => false;
    protected virtual bool UseNativeLoadBalancing => true;
    protected virtual void ConfigureHttpClient(IHttpClientBuilder builder) { }
    public override string[] OptionKeys => ["Grpc:Client:Default", OptionsKey];

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TGrpcClientModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        if (startupOptions.EnableHttp2UnencryptedSupport)
        {
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        services.TryAddSingleton(TimeProvider.System);
        var builder = services.AddGrpcClient<TClient>(GrpcServicesHelper.GetServiceNameForClient<TClient>(), (provider, options) =>
        {
            options.Address = GenerateAddress(startupOptions);
            options.CallOptionsActions.Add(context =>
            {
                if (context.CallOptions.Deadline is null && startupOptions.DefaultDeadline is not null)
                {
                    context.CallOptions = context.CallOptions.WithDeadline(provider.GetRequiredService<TimeProvider>()
                        .GetUtcNow()
                        .Add(startupOptions.DefaultDeadline.Value).UtcDateTime);
                }
            });
        });
        ConfigureHttpClient(builder);
        services.AddTransient<IGrpcClientProvider<TClient>, GrpcClientProvider<TClient>>();
        startupOptions.ConfigureClient(services, builder);
        builder.ConfigurePrimaryHttpMessageHandler(provider =>
            NeedSocketHandler
                ? CreateSocketHttpHandler(provider, startupOptions)
                : CreateHttpClientHandler(provider, startupOptions));
        builder.ConfigureChannel(options =>
        {
            if (!startupOptions.EnableHttp2UnencryptedSupport)
            {
                options.Credentials = ChannelCredentials.SecureSsl;
            }

            options.ServiceConfig = new ServiceConfig();

            var loadBalancingConfig = startupOptions.GetLoadBalancingConfig();
            if (loadBalancingConfig is not null && NeedSocketHandler && UseNativeLoadBalancing)
            {
                options.ServiceConfig.LoadBalancingConfigs.Add(loadBalancingConfig);
            }

            if (startupOptions.RetryPolicy is not null)
            {
                options.ServiceConfig.MethodConfigs.Add(
                    new MethodConfig { Names = { MethodName.Default }, RetryPolicy = startupOptions.RetryPolicy }
                );
            }

            options.MaxReceiveMessageSize = startupOptions.MaxReceiveMessageSize;
            options.MaxSendMessageSize = startupOptions.MaxSendMessageSize;

            startupOptions.ConfigureChannelOptions?.Invoke(options);
        });
    }

    public OpenTelemetryBuilder ConfigureOpenTelemetry(IApplicationContext context, TGrpcClientModuleOptions options,
        OpenTelemetryBuilder builder) =>
        builder.WithTracing(providerBuilder => providerBuilder.AddGrpcClientInstrumentation());

    protected abstract Uri GenerateAddress(TGrpcClientModuleOptions options);

    protected virtual HttpMessageHandler CreateHttpClientHandler(IServiceProvider provider,
        TGrpcClientModuleOptions options)
    {
        var httpClientHandler = new HttpClientHandler();
        if (options.DisableCertificatesValidation)
        {
            httpClientHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        HttpMessageHandler handler = httpClientHandler;
        if (options.ConfigureHttpHandler is not null)
        {
            handler = options.ConfigureHttpHandler(handler);
        }

        return options.UseGrpcWeb ? new GrpcWebHandler(handler) : handler;
    }

    protected virtual HttpMessageHandler CreateSocketHttpHandler(IServiceProvider serviceProvider,
        TGrpcClientModuleOptions options)
    {
        var handler = new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromSeconds(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            EnableMultipleHttp2Connections = true
        };

        if (options.PooledConnectionLifetime is not null)
        {
            handler.PooledConnectionLifetime = options.PooledConnectionLifetime.Value;
        }

        if (options.DisableCertificatesValidation)
        {
#pragma warning disable CA5359
            handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
#pragma warning restore CA5359
        }

        var result = options.ConfigureHttpHandler?.Invoke(handler) ?? handler;
        return options.UseGrpcWeb ? new GrpcWebHandler(result) : result;
    }
}

public class GrpcClientModule<TClient> : GrpcClientModule<TClient, GrpcClientModuleOptions<TClient>>
    where TClient : ClientBase<TClient>
{
    private bool useDiscovery;
    public override string[] OptionKeys => ["Grpc:Client"];
    public override string OptionsKey => $"Grpc:Client:Contracts:{GrpcServicesHelper.GetServiceNameForClient<TClient>()}";
    protected override bool NeedSocketHandler => true;
    protected override bool UseNativeLoadBalancing => false;

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        GrpcClientModuleOptions<TClient> startupOptions)
    {
        if (startupOptions.Address is { } address &&
            (!address.IsAbsoluteUri || (address.Scheme != Uri.UriSchemeHttps && address.Scheme != Uri.UriSchemeHttp) ||
             string.IsNullOrWhiteSpace(address.Host)))
        {
            throw new InvalidOperationException($"Contract {GrpcServicesHelper.GetServiceNameForClient<TClient>()} has invalid Address: expected an absolute HTTP(S) URI");
        }
        useDiscovery = startupOptions.Address is null;
        if (useDiscovery)
        {
            if (applicationContext.Configuration.GetValue<bool?>("ServiceDiscovery:Enabled") == false)
            {
                throw new InvalidOperationException($"Contract {GrpcServicesHelper.GetServiceNameForClient<TClient>()} requires Address or ServiceDiscovery.Enabled=true");
            }
            startupOptions.Address = new Uri($"https://{ServiceDiscoveryName.Create(applicationContext.Environment, GrpcServicesHelper.GetServiceNameForClient<TClient>())}");
        }
        base.ConfigureServices(applicationContext, services, startupOptions);
    }

    public override void PostConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        GrpcClientModuleOptions<TClient> startupOptions)
    {
        if (useDiscovery && !services.Any(static service => service.ServiceType == typeof(IServiceEndpointProviderFactory)))
        {
            throw new InvalidOperationException($"Contract {GrpcServicesHelper.GetServiceNameForClient<TClient>()} requires a ServiceDiscovery provider or Address");
        }
    }

    protected override Uri GenerateAddress(GrpcClientModuleOptions<TClient> options) => options.Address!;

    protected override void ConfigureHttpClient(IHttpClientBuilder builder)
    {
        if (useDiscovery)
        {
            _ = builder.AddServiceDiscovery();
        }
    }
}
