using Grpc.Core;
using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Client.Consul;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddConsulGrpcClient<TClient>(this Application application,
        Action<IApplicationContext, ConsulGrpcClientModuleOptions<TClient>> configure,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        application.AddModule<ConsulGrpcClientModule<TClient>, ConsulGrpcClientModuleOptions<TClient>>(configure,
            optionsKey);

    public static Application AddConsulGrpcClient<TClient>(this Application application,
        Action<ConsulGrpcClientModuleOptions<TClient>>? configure = null,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        application.AddModule<ConsulGrpcClientModule<TClient>, ConsulGrpcClientModuleOptions<TClient>>(configure,
            optionsKey);
}

