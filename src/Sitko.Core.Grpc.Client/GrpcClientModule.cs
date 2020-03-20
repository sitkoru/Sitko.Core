using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Consul;

namespace Sitko.Core.Grpc.Client
{
    public class GrpcClientModule : BaseApplicationModule<GrpcClientModuleConfig>
    {
        public GrpcClientModule(GrpcClientModuleConfig config, Application application) : base(config, application)
        {
        }

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

    public class GrpcClientModuleConfig
    {
        public bool EnableHttp2UnencryptedSupport { get; set; }
        public bool DisableCertificatesValidation { get; set; }
    }
}
