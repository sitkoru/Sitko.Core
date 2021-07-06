using System;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Grpc.Client.External;

namespace Sitko.Core.Grpc.Client
{
    public static class ApplicationExtensions
    {
        public static Application AddExternalGrpcClient<TClient>(this Application application,
            Action<IConfiguration, IHostEnvironment, ExternalGrpcClientModuleOptions> configure,
            string? optionsKey = null)
            where TClient : ClientBase<TClient>
        {
            return application.AddModule<ExternalGrpcClientModule<TClient>, ExternalGrpcClientModuleOptions>(configure,
                optionsKey);
        }

        public static Application AddExternalGrpcClient<TClient>(this Application application,
            Action<ExternalGrpcClientModuleOptions>? configure = null,
            string? optionsKey = null)
            where TClient : ClientBase<TClient>
        {
            return application.AddModule<ExternalGrpcClientModule<TClient>, ExternalGrpcClientModuleOptions>(configure,
                optionsKey);
        }
    }
}
