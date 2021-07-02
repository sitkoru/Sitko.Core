using System;
using System.Collections.Generic;
using Grpc.Core;
using Sitko.Core.App;
using Sitko.Core.Consul;

namespace Sitko.Core.Grpc.Client.Consul
{
    public class GrpcClientConsulModule<TClient> : GrpcClientModule<TClient, ConsulGrpcServiceAddressResolver<TClient>,
        GrpcClientConsulModuleOptions>
        where TClient : ClientBase<TClient>
    {
        public override string GetOptionsKey()
        {
            return "Grpc:Client:Consul";
        }

        public override IEnumerable<Type> GetRequiredModules(ApplicationContext context,
            GrpcClientConsulModuleOptions options)
        {
            return new List<Type> {typeof(IConsulModule)};
        }
    }

    public class GrpcClientConsulModuleOptions : GrpcClientModuleOptions
    {
    }
}
