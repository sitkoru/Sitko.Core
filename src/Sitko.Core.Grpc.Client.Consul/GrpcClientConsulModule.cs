using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Consul;

namespace Sitko.Core.Grpc.Client.Consul
{
    public class GrpcClientConsulModule : GrpcClientModule
    {
        public GrpcClientConsulModule(GrpcClientModuleConfig config, Application application) :
            base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton(typeof(IGrpcClientProvider<>), typeof(ConsulGrpcClientProvider<>));
        }

        public override List<Type> GetRequiredModules()
        {
            return new List<Type> {typeof(ConsulModule)};
        }
    }
}
