using System;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddGrpcServer(this Application application,
        Action<IConfiguration, IAppEnvironment, GrpcServerModuleOptions> configure,
        string? optionsKey = null) =>
        application.AddModule<GrpcServerModule, GrpcServerModuleOptions>(configure, optionsKey);

    public static Application AddGrpcServer(this Application application,
        Action<GrpcServerModuleOptions>? configure = null,
        string? optionsKey = null) =>
        application.AddModule<GrpcServerModule, GrpcServerModuleOptions>(configure, optionsKey);
}
