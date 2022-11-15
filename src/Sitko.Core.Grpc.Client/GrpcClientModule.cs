using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sitko.Core.App;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client;

public interface IGrpcClientModule<TClient> where TClient : ClientBase<TClient>
{
}

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

        services.AddSingleton<IGrpcClientProvider<TClient>, GrpcClientProvider<TClient, TGrpcClientModuleOptions>>();
        RegisterResolver(services, startupOptions);
        if (startupOptions.Interceptors.Any())
        {
            foreach (var type in startupOptions.Interceptors)
            {
                services.TryAddSingleton(type);
            }
        }

        services.AddHealthChecks()
            .AddCheck<GrpcClientHealthCheck<TClient>>($"GRPC Client check: {typeof(TClient)}");
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

