using System;
using System.Collections.Generic;
using Grpc.Core;
using Sitko.Core.App;
using Sitko.Core.Consul;

namespace Sitko.Core.Grpc.Client.Consul;

public class ConsulGrpcClientModule<TClient> : GrpcClientModule<TClient, ConsulGrpcServiceAddressResolver<TClient>,
    ConsulGrpcClientModuleOptions<TClient>>
    where TClient : ClientBase<TClient>
{
    public override string OptionsKey => "Grpc:Client:Consul";

    public override IEnumerable<Type> GetRequiredModules(IApplicationContext context,
        ConsulGrpcClientModuleOptions<TClient> options) =>
        new List<Type> { typeof(ConsulModule) };
}

public class ConsulGrpcClientModuleOptions<TClient> : GrpcClientModuleOptions<TClient>
    where TClient : ClientBase<TClient>
{
}
