using JetBrains.Annotations;

namespace Sitko.Core.Grpc.Client
{
    using System;
    using App;
    using External;
    using global::Grpc.Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;

    [PublicAPI]
    public static class ApplicationExtensions
    {
        public static Application AddExternalGrpcClient<TClient>(this Application application,
            Action<IConfiguration, IHostEnvironment, ExternalGrpcClientModuleOptions<TClient>> configure,
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
            application.AddExternalGrpcClient<TApplication, TClient>(new Uri(address), configure);

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
            Func<IConfiguration, IHostEnvironment, Uri> getAddress,
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
}
