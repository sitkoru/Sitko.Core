namespace Sitko.Core.Grpc.Server
{
    using System;
    using App;
    using JetBrains.Annotations;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;

    [PublicAPI]
    public static class ApplicationExtensions
    {
        public static Application AddGrpcServer(this Application application,
            Action<IConfiguration, IHostEnvironment, GrpcServerModuleOptions> configure,
            string? optionsKey = null) =>
            application.AddModule<GrpcServerModule, GrpcServerModuleOptions>(configure, optionsKey);

        public static Application AddGrpcServer(this Application application,
            Action<GrpcServerModuleOptions>? configure = null,
            string? optionsKey = null) =>
            application.AddModule<GrpcServerModule, GrpcServerModuleOptions>(configure, optionsKey);
    }
}
