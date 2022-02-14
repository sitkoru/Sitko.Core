using System;
using System.Collections.Generic;
using Grpc.Core;
using Sitko.Core.App;
using Sitko.Core.Consul;
using Sitko.Core.Grpc.Client.External;

namespace Sitko.Core.Grpc.Client.Web;

public class GrpcWebClientModule<TClient> : GrpcClientModule<TClient, ExternalGrpcServiceAddressResolver<TClient>,
    GrpcWebClientModuleOptions<TClient>>
    where TClient : ClientBase<TClient>
{
    public override string OptionsKey => "Grpc:Client:Web";

    public override IEnumerable<Type> GetRequiredModules(IApplicationContext context,
        GrpcWebClientModuleOptions<TClient> options) =>
        new List<Type> {typeof(ConsulModule)};
}

public class GrpcWebClientModuleOptions<TClient> : GrpcClientModuleOptions<TClient>
    where TClient : ClientBase<TClient>
{
}
