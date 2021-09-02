namespace Sitko.Core.Grpc.Client
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using App;
    using Discovery;
    using global::Grpc.Core;
    using global::Grpc.Core.Interceptors;
    using Microsoft.Extensions.DependencyInjection;

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
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TGrpcClientModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            if (startupOptions.EnableHttp2UnencryptedSupport)
            {
                AppContext.SetSwitch(
                    "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            }

            services.AddSingleton<IGrpcClientProvider<TClient>, GrpcClientProvider<TClient>>();
            RegisterResolver(services, startupOptions);
            if (startupOptions.Interceptors.Any())
            {
                foreach (var type in startupOptions.Interceptors)
                {
                    services.AddSingleton(type);
                }
            }

            services.AddGrpcClient<TClient>((provider, options) =>
                {
                    var config = GetOptions(provider);
                    if (config.Interceptors.Any())
                    {
                        foreach (var service in config.Interceptors.Select(provider.GetService))
                        {
                            if (service is Interceptor interceptor)
                            {
                                options.Interceptors.Add(interceptor);
                            }
                        }
                    }

                    var resolver = provider.GetRequiredService<IGrpcServiceAddressResolver<TClient>>();
                    options.Address = resolver.GetAddress();
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    var handler = new HttpClientHandler();
                    if (startupOptions.DisableCertificatesValidation)
                    {
                        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                    }

                    return handler;
                }).ConfigureChannel(options =>
                {
                    startupOptions.ConfigureChannelOptions?.Invoke(options);
                });
            services.AddHealthChecks()
                .AddCheck<GrpcClientHealthCheck<TClient>>($"GRPC Client check: {typeof(TClient)}");
        }

        public override async Task InitAsync(ApplicationContext context, IServiceProvider serviceProvider)
        {
            await base.InitAsync(context, serviceProvider);
            var resolver = serviceProvider.GetRequiredService<IGrpcServiceAddressResolver<TClient>>();
            await resolver.InitAsync();
        }

        protected virtual void RegisterResolver(IServiceCollection services, TGrpcClientModuleOptions config) =>
            services.AddSingleton<IGrpcServiceAddressResolver<TClient>, TResolver>();
    }
}
