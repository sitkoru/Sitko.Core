using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sitko.Core.App;
using Sitko.Core.App.Health;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client;

public interface IGrpcClientModule<TClient> where TClient : ClientBase<TClient>;

public abstract class GrpcClientModule<TClient, TResolver, TGrpcClientModuleOptions> :
    BaseApplicationModule<TGrpcClientModuleOptions>,
    IGrpcClientModule<TClient>
    where TClient : ClientBase<TClient>
    where TResolver : class, IGrpcServiceAddressResolver<TClient>
    where TGrpcClientModuleOptions : GrpcClientModuleOptions<TClient>, new()
{
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
        services.TryAddSingleton<GrpcClientActivator<TClient>>();
        services.TryAddSingleton<GrpcCallInvokerFactory>();
        services.TryAddTransient<global::Grpc.Net.ClientFactory.GrpcClientFactory, GrpcClientFactory>();
        var builder = services.AddGrpcClient<TClient>((provider, options) =>
        {
            options.CallOptionsActions.Add(context =>
            {
                if (startupOptions.DefaultDeadline is not null)
                {
                    context.CallOptions = context.CallOptions.WithDeadline(provider.GetRequiredService<TimeProvider>()
                        .GetUtcNow()
                        .Add(startupOptions.DefaultDeadline.Value).DateTime);
                }
            });
        });
        services.AddTransient<IGrpcClientProvider<TClient>, GrpcClientProvider<TClient>>();
        RegisterResolver(services, startupOptions);
        startupOptions.ConfigureClient(services, builder);
        builder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            if (startupOptions.DisableCertificatesValidation)
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            if (startupOptions.ConfigureHttpHandler is not null)
            {
                return startupOptions.ConfigureHttpHandler(handler);
            }

            return handler;
        });
        builder.ConfigureChannel(options =>
        {
            startupOptions.ConfigureChannelOptions?.Invoke(options);
        });

        services.AddHealthChecks()
            .AddCheck<GrpcClientHealthCheck<TClient>>($"GRPC Client check: {typeof(TClient)}",
                tags: HealthCheckStages.GetSkipAllTags());
    }

    public override async Task InitAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider)
    {
        await base.InitAsync(applicationContext, serviceProvider);
        var resolver = serviceProvider.GetRequiredService<IGrpcServiceAddressResolver<TClient>>();
        await resolver.InitAsync();
    }

    protected virtual void RegisterResolver(IServiceCollection services, TGrpcClientModuleOptions config) =>
        services.AddSingleton<IGrpcServiceAddressResolver<TClient>, TResolver>();
}
