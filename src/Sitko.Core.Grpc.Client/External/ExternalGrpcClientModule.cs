namespace Sitko.Core.Grpc.Client.External
{
    using System;
    using Discovery;
    using global::Grpc.Core;
    using Microsoft.Extensions.DependencyInjection;

    public class ExternalGrpcClientModule<TClient> : GrpcClientModule<TClient,
        ExternalGrpcServiceAddressResolver<TClient>,
        ExternalGrpcClientModuleOptions>
        where TClient : ClientBase<TClient>
    {
        public override string OptionsKey => "Grpc:Client:External";

        protected override void RegisterResolver(IServiceCollection services,
            ExternalGrpcClientModuleOptions options) =>
            services.AddSingleton<IGrpcServiceAddressResolver<TClient>>(
                new ExternalGrpcServiceAddressResolver<TClient>(options.Address));
    }

    public class ExternalGrpcClientModuleOptions : GrpcClientModuleOptions
    {
        public Uri Address { get; set; } = new("http://localhost");
    }
}
