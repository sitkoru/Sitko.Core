using System;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client.External
{
    public class ExternalGrpcClientModule<TClient> : GrpcClientModule<TClient, ExternalGrpcServiceAddressResolver<TClient>,
        GrpcClientStaticModuleConfig>
        where TClient : ClientBase<TClient>
    {
        public ExternalGrpcClientModule(GrpcClientStaticModuleConfig config, Application application) :
            base(config, application)
        {
        }

        protected override void RegisterResolver(IServiceCollection services)
        {
            services.AddSingleton<IGrpcServiceAddressResolver<TClient>>(
                new ExternalGrpcServiceAddressResolver<TClient>(Config.Address));
        }
    }

    public class GrpcClientStaticModuleConfig : GrpcClientModuleConfig
    {
        public Uri Address { get; set; }
    }
}
