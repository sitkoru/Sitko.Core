using System;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client.External
{
    public class ExternalGrpcClientModule<TClient> : GrpcClientModule<TClient,
        ExternalGrpcServiceAddressResolver<TClient>,
        GrpcClientStaticModuleOptions>
        where TClient : ClientBase<TClient>
    {
        protected override void RegisterResolver(IServiceCollection services,
            GrpcClientStaticModuleOptions options)
        {
            services.AddSingleton<IGrpcServiceAddressResolver<TClient>>(
                new ExternalGrpcServiceAddressResolver<TClient>(options.Address));
        }

        public override string GetOptionsKey()
        {
            return "Grpc:Client:External";
        }
    }

    public class GrpcClientStaticModuleOptions : GrpcClientModuleOptions
    {
        public Uri Address { get; set; }
    }
}
