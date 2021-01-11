using System;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Client.External
{
    public static class ExternalGrpcClientExtensions
    {
        public static TApplication AddExternalGrpcClient<TApplication, TClient>(this TApplication application,
            string address,
            Action<GrpcClientModuleConfig>? configure = null)
            where TApplication : Application<TApplication> where TClient : ClientBase<TClient>
        {
            return application.AddExternalGrpcClient<TApplication, TClient>(new Uri(address), configure);
        }

        public static TApplication AddExternalGrpcClient<TApplication, TClient>(this TApplication application,
            Uri address,
            Action<GrpcClientModuleConfig>? configure = null)
            where TApplication : Application<TApplication> where TClient : ClientBase<TClient>
        {
            application.AddModule<ExternalGrpcClientModule<TClient>, GrpcClientStaticModuleConfig>((configuration,
                environment, moduleConfig) =>
            {
                moduleConfig.Address = address;
                configure?.Invoke(moduleConfig);
            });
            return application;
        }

        public static TApplication AddExternalGrpcClient<TApplication, TClient>(this TApplication application,
            Func<IConfiguration, IHostEnvironment, Uri> getAddress,
            Action<GrpcClientModuleConfig>? configure = null)
            where TApplication : Application<TApplication> where TClient : ClientBase<TClient>
        {
            application.AddModule<ExternalGrpcClientModule<TClient>, GrpcClientStaticModuleConfig>((configuration,
                environment, moduleConfig) =>
            {
                moduleConfig.Address = getAddress(configuration, environment);
                configure?.Invoke(moduleConfig);
            });
            return application;
        }
    }
}
