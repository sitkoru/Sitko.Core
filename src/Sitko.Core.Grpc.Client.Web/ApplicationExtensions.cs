using System;
using Grpc.Core;
using Grpc.Net.Client.Web;
using JetBrains.Annotations;
using Sitko.Core.App;
using Sitko.Core.Grpc.Client.External;

namespace Sitko.Core.Grpc.Client.Web;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddGrpcWebClient<TClient>(this Application application,
        Action<IApplicationContext, ExternalGrpcClientModuleOptions<TClient>> configure,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        application.AddModule<ExternalGrpcClientModule<TClient>, ExternalGrpcClientModuleOptions<TClient>>((context, options) =>
            {
                options.ConfigureHttpHandler = handler => new GrpcWebHandler(handler);
                configure(context, options);
            },
            optionsKey);

    public static Application AddGrpcWebClient<TClient>(this Application application,
        Action<ExternalGrpcClientModuleOptions<TClient>>? configure = null,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        application.AddModule<ExternalGrpcClientModule<TClient>, ExternalGrpcClientModuleOptions<TClient>>(options =>
            {
                options.ConfigureHttpHandler = handler => new GrpcWebHandler(handler);
                configure?.Invoke(options);
            },
            optionsKey);
}
