namespace Sitko.Core.Grpc.Client.Consul
{
    using System;
    using System.Collections.Generic;
    using App;
    using Core.Consul;
    using global::Grpc.Core;

    public class ConsulGrpcClientModule<TClient> : GrpcClientModule<TClient, ConsulGrpcServiceAddressResolver<TClient>,
        ConsulGrpcClientModuleOptions>
        where TClient : ClientBase<TClient>
    {
        public override string OptionsKey => "Grpc:Client:Consul";

        public override IEnumerable<Type> GetRequiredModules(ApplicationContext context,
            ConsulGrpcClientModuleOptions options) =>
            new List<Type> {typeof(IConsulModule)};
    }

    public class ConsulGrpcClientModuleOptions : GrpcClientModuleOptions
    {}
}
