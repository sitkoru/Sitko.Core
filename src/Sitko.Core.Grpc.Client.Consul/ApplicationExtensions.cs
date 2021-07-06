using System;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Client.Consul
{
    public static class ApplicationExtensions
    {
        public static Application AddConsulGrpcClient<TClient>(this Application application,
            Action<IConfiguration, IHostEnvironment, ConsulGrpcClientModuleOptions> configure,
            string? optionsKey = null)
            where TClient : ClientBase<TClient>
        {
            return application.AddModule<ConsulGrpcClientModule<TClient>, ConsulGrpcClientModuleOptions>(configure,
                optionsKey);
        }

        public static Application AddConsulGrpcClient<TClient>(this Application application,
            Action<ConsulGrpcClientModuleOptions>? configure = null,
            string? optionsKey = null)
            where TClient : ClientBase<TClient>
        {
            return application.AddModule<ConsulGrpcClientModule<TClient>, ConsulGrpcClientModuleOptions>(configure,
                optionsKey);
        }
    }
}
