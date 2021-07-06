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
            Action<GrpcClientModuleOptions>? configure = null)
            where TApplication : Application where TClient : ClientBase<TClient>
        {
            return application.AddExternalGrpcClient<TApplication, TClient>(new Uri(address), configure);
        }

        public static TApplication AddExternalGrpcClient<TApplication, TClient>(this TApplication application,
            Uri address,
            Action<GrpcClientModuleOptions>? configure = null)
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
            Action<GrpcClientModuleOptions>? configure = null)
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
