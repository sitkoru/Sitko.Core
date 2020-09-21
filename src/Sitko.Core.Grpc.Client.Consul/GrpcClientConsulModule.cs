using System;
using System.Collections.Generic;
using Grpc.Core;
using Sitko.Core.App;
using Sitko.Core.Consul;

namespace Sitko.Core.Grpc.Client.Consul
{
    public class GrpcClientConsulModule<TClient> : GrpcClientModule<TClient, ConsulGrpcServiceAddressResolver<TClient>>
        where TClient : ClientBase<TClient>
    {
        public GrpcClientConsulModule(GrpcClientModuleConfig config, Application application) :
            base(config, application)
        {
        }

        public override List<Type> GetRequiredModules()
        {
            return new List<Type> {typeof(IConsulModule)};
        }
    }
}
