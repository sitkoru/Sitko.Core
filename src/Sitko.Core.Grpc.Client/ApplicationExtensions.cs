using System;
using Grpc.Core;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Sitko.Core.App;
using Sitko.Core.Grpc.Client.External;

namespace Sitko.Core.Grpc.Client;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddExternalGrpcClient<TClient>(this Application application,
        Action<IConfiguration, IAppEnvironment, ExternalGrpcClientModuleOptions<TClient>> configure,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        application.AddModule<ExternalGrpcClientModule<TClient>, ExternalGrpcClientModuleOptions<TClient>>(configure,
            optionsKey);

    public static Application AddExternalGrpcClient<TClient>(this Application application,
        Action<ExternalGrpcClientModuleOptions<TClient>>? configure = null,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        application.AddModule<ExternalGrpcClientModule<TClient>, ExternalGrpcClientModuleOptions<TClient>>(configure,
            optionsKey);

    public static TApplication AddExternalGrpcClient<TApplication, TClient>(this TApplication application,
        string address,
        Action<GrpcClientModuleOptions<TClient>>? configure = null)
        where TApplication : Application where TClient : ClientBase<TClient> =>
        application.AddExternalGrpcClient(new Uri(address), configure);

    public static TApplication AddExternalGrpcClient<TApplication, TClient>(this TApplication application,
        Uri address,
        Action<GrpcClientModuleOptions<TClient>>? configure = null)
        where TApplication : Application where TClient : ClientBase<TClient>
    {
        application.AddExternalGrpcClient<TClient>(moduleOptions =>
        {
            moduleOptions.Address = address;
            configure?.Invoke(moduleOptions);
        });
        return application;
    }

    public static TApplication AddExternalGrpcClient<TApplication, TClient>(this TApplication application,
        Func<IConfiguration, IAppEnvironment, Uri> getAddress,
        Action<GrpcClientModuleOptions<TClient>>? configure = null)
        where TApplication : Application where TClient : ClientBase<TClient>
    {
        application.AddExternalGrpcClient<TClient>((configuration, environment, moduleOptions) =>
        {
            moduleOptions.Address = getAddress(configuration, environment);
            configure?.Invoke(moduleOptions);
        });
        return application;
    }
}
