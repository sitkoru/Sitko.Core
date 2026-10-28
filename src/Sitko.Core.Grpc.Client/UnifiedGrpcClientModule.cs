using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.ServiceDiscovery;
using Sitko.Core.App;
using Sitko.Core.ServiceDiscovery;

namespace Sitko.Core.Grpc.Client
{
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
}
