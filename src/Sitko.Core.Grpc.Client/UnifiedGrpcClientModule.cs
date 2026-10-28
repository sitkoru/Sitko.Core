using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.ServiceDiscovery;

namespace Sitko.Core.Grpc.Client
{
    public class GrpcClientModule<TClient> : GrpcClientModule<TClient, GrpcClientModuleOptions<TClient>>
        where TClient : ClientBase<TClient>
    {
        private bool useDiscovery;
        public override string OptionsKey => $"Grpc:Client:Contracts:{GrpcServicesHelper.GetServiceNameForClient<TClient>()}";
        protected override bool NeedSocketHandler => true;
        protected override bool UseNativeLoadBalancing => false;

        public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
            GrpcClientModuleOptions<TClient> startupOptions)
        {
            useDiscovery = startupOptions.Address is null;
            if (useDiscovery)
            {
                startupOptions.Address = new Uri($"https://{ServiceDiscoveryName.Create(applicationContext.Environment, GrpcServicesHelper.GetServiceNameForClient<TClient>())}");
            }
            base.ConfigureServices(applicationContext, services, startupOptions);
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
}
