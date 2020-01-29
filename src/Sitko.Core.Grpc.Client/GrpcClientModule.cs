using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Consul;

namespace Sitko.Core.Grpc.Client
{
    public class GrpcClientModule : BaseApplicationModule
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton(typeof(IGrpcClientProvider<>), typeof(GrpcClientProvider<>));
        }

        public override List<Type> GetRequiredModules()
        {
            return new List<Type> {typeof(ConsulModule)};
        }
    }

    public static class GrpcClientModuleExtensions
    {
        public static T AddGrpcClient<T>(this T application) where T : Application<T>
        {
            return application.AddModule<GrpcClientModule>();
        }
    }
}
