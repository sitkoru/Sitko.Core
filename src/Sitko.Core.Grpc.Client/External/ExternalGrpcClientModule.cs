using System;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Grpc.Client.Discovery;

namespace Sitko.Core.Grpc.Client.External
{
    public class ExternalGrpcClientModule<TClient> : GrpcClientModule<TClient,
        ExternalGrpcServiceAddressResolver<TClient>,
        ExternalGrpcClientModuleOptions>
        where TClient : ClientBase<TClient>
    {
        protected override void RegisterResolver(IServiceCollection services,
            ExternalGrpcClientModuleOptions options) =>
            services.AddSingleton<IGrpcServiceAddressResolver<TClient>>(
                new ExternalGrpcServiceAddressResolver<TClient>(options.Address));

        public override string OptionsKey => "Grpc:Client:External";
    }

    public class ExternalGrpcClientModuleOptions : GrpcClientModuleOptions
    {
        public Uri Address { get; set; } = new("http://localhost");
    }
}
