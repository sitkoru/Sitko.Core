using System;
using Grpc.Core;
using Grpc.Net.Client.Web;
using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Client.Web;

[PublicAPI]
public static class ApplicationExtensions
{
    public static Application AddGrpcWebClient<TClient>(this Application application,
        Action<IApplicationContext, GrpcWebClientModuleOptions<TClient>> configure,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        application.AddModule<GrpcWebClientModule<TClient>, GrpcWebClientModuleOptions<TClient>>((context, options) =>
            {
                options.ConfigureHttpHandler = handler => new GrpcWebHandler(handler);
                configure(context, options);
            },
            optionsKey);

    public static Application AddGrpcWebClient<TClient>(this Application application,
        Action<GrpcWebClientModuleOptions<TClient>>? configure = null,
        string? optionsKey = null)
        where TClient : ClientBase<TClient> =>
        application.AddModule<GrpcWebClientModule<TClient>, GrpcWebClientModuleOptions<TClient>>(options =>
            {
                options.ConfigureHttpHandler = handler => new GrpcWebHandler(handler);
                configure?.Invoke(options);
            },
            optionsKey);
}
