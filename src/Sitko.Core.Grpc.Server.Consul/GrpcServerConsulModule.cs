using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Grpc.Server.Consul
{
    public class GrpcServerConsulModule : BaseApplicationModule
    {
        public GrpcServerConsulModule(BaseApplicationModuleConfig config, Application application) : base(config,
            application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IGrpcServicesRegistrar, GrpcServicesRegistrar>();
        }
    }
}
