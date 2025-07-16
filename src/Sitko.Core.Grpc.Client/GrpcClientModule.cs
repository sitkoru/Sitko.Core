using Grpc.Core;
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
        var builder = services.AddGrpcClient<TClient>((provider, options) =>
        {
            options.Address = GenerateAddress(startupOptions);
            options.CallOptionsActions.Add(context =>
            {
                if (startupOptions.DefaultDeadline is not null)
                {
                    context.CallOptions = context.CallOptions.WithDeadline(provider.GetRequiredService<TimeProvider>()
                        .GetUtcNow()
                        .Add(startupOptions.DefaultDeadline.Value).UtcDateTime);
                }
            });
        });
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

            if (NeedSocketHandler)
            {
                options.ServiceConfig.LoadBalancingConfigs.Add(new RoundRobinConfig());
            }

            if (startupOptions.RetryPolicy is not null)
            {
                options.ServiceConfig.MethodConfigs.Add(
                    new MethodConfig { Names = { MethodName.Default }, RetryPolicy = startupOptions.RetryPolicy }
                );
            }

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

        if (options.ConfigureHttpHandler is not null)
        {
            return options.ConfigureHttpHandler(handler);
        }

        return handler;
    }
}
