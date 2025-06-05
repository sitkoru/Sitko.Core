using Grpc.Core;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
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
    protected abstract string ResolverFactoryScheme { get; }

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
        services.AddSingleton(CreateResolverFactory);
        RegisterClient<TClient>(applicationContext, startupOptions);
        var builder = services.AddGrpcClient<TClient>((provider, options) =>
        {
            options.Address =
                new Uri($"{ResolverFactoryScheme}:///{GrpcServicesHelper.GetServiceNameForClient<TClient>()}");
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
        builder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new SocketsHttpHandler();

            if (startupOptions.DisableCertificatesValidation)
            {
#pragma warning disable CA5359
                handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
#pragma warning restore CA5359
            }

            if (startupOptions.ConfigureHttpHandler is not null)
            {
                return startupOptions.ConfigureHttpHandler(handler);
            }

            return handler;
        });
        builder.ConfigureChannel(options =>
        {
            if (!startupOptions.EnableHttp2UnencryptedSupport)
            {
                options.Credentials = ChannelCredentials.SecureSsl;
            }

            options.ServiceConfig = new ServiceConfig { LoadBalancingConfigs = { new RoundRobinConfig() } };

            startupOptions.ConfigureChannelOptions?.Invoke(options);
        });
    }

    public OpenTelemetryBuilder ConfigureOpenTelemetry(IApplicationContext context, TGrpcClientModuleOptions options,
        OpenTelemetryBuilder builder) =>
        builder.WithTracing(providerBuilder => providerBuilder.AddGrpcClientInstrumentation());

    protected virtual void RegisterClient<TClientBase>(IApplicationContext applicationContext,
        TGrpcClientModuleOptions options)
        where TClientBase : ClientBase<TClientBase>
    {
    }

    protected abstract ResolverFactory CreateResolverFactory(IServiceProvider sp);
}
