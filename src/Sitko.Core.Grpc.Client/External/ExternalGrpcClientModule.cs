using System;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client.External
{
    public class ExternalGrpcClientModule<TClient> : GrpcClientModule<TClient,
        ExternalGrpcServiceAddressResolver<TClient>,
        GrpcClientStaticModuleConfig>
        where TClient : ClientBase<TClient>
    {
        protected override void RegisterResolver(IServiceCollection services,
            GrpcClientStaticModuleConfig config)
        {
            services.AddSingleton<IGrpcServiceAddressResolver<TClient>>(
                new ExternalGrpcServiceAddressResolver<TClient>(config.Address));
        }

        public override string GetConfigKey()
        {
            return "Grpc:Client:External";
        }
    }

    public class GrpcClientStaticModuleConfig : GrpcClientModuleConfig
    {
        public Uri Address { get; set; }
    }
}
