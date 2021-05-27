using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client
{
    public interface IGrpcClientModule<TClient> where TClient : ClientBase<TClient>
    {
    }

    public abstract class GrpcClientModule<TClient, TResolver, TConfig> : BaseApplicationModule<TConfig>,
        IGrpcClientModule<TClient>
        where TClient : ClientBase<TClient>
        where TResolver : class, IGrpcServiceAddressResolver<TClient>
        where TConfig : GrpcClientModuleConfig, new()
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TConfig startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            if (startupConfig.EnableHttp2UnencryptedSupport)
            {
                AppContext.SetSwitch(
                    "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            }

            services.AddSingleton<IGrpcClientProvider<TClient>, GrpcClientProvider<TClient>>();
            RegisterResolver(services, startupConfig);
            if (startupConfig.Interceptors.Any())
            {
                foreach (var type in startupConfig.Interceptors)
                {
                    services.AddSingleton(type);
                }
            }

            services.AddGrpcClient<TClient>((provider, options) =>
                {
                    var config = GetConfig(provider);
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
                    if (startupConfig.DisableCertificatesValidation)
                    {
                        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                    }

                    return handler;
                }).ConfigureChannel(options =>
                {
                    startupConfig.ConfigureChannelOptions?.Invoke(options);
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

        protected virtual void RegisterResolver(IServiceCollection services, TConfig config)
        {
            services.AddSingleton<IGrpcServiceAddressResolver<TClient>, TResolver>();
        }
    }
}
