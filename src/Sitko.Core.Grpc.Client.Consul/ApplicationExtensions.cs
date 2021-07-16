namespace Sitko.Core.Grpc.Client.Consul
{
    using System;
    using App;
    using global::Grpc.Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;

    public static class ApplicationExtensions
    {
        public static Application AddConsulGrpcClient<TClient>(this Application application,
            Action<IConfiguration, IHostEnvironment, ConsulGrpcClientModuleOptions> configure,
            string? optionsKey = null)
            where TClient : ClientBase<TClient> =>
            application.AddModule<ConsulGrpcClientModule<TClient>, ConsulGrpcClientModuleOptions>(configure,
                optionsKey);

        public static Application AddConsulGrpcClient<TClient>(this Application application,
            Action<ConsulGrpcClientModuleOptions>? configure = null,
            string? optionsKey = null)
            where TClient : ClientBase<TClient> =>
            application.AddModule<ConsulGrpcClientModule<TClient>, ConsulGrpcClientModuleOptions>(configure,
                optionsKey);
    }
}
