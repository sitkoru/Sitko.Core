using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server
{
    public static class ApplicationExtensions
    {
        public static Application AddGrpcServer(this Application application,
            Action<IConfiguration, IHostEnvironment, GrpcServerModuleOptions> configure,
            string? optionsKey = null)
        {
            return application.AddModule<GrpcServerModule, GrpcServerModuleOptions>(configure, optionsKey);
        }

        public static Application AddGrpcServer(this Application application,
            Action<GrpcServerModuleOptions>? configure = null,
            string? optionsKey = null)
        {
            return application.AddModule<GrpcServerModule, GrpcServerModuleOptions>(configure, optionsKey);
        }
    }
}
