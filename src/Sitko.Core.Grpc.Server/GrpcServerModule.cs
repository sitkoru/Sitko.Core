using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Grpc.Server
{
    public class GrpcServerModule : BaseApplicationModule<GrpcServerOptions>, IWebApplicationModule
    {
        public GrpcServerModule(GrpcServerOptions config, Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddGrpc(options =>
            {
                options.EnableDetailedErrors = environment.IsDevelopment();
            });
        }

        public void ConfigureEndpoints(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGrpcService<HealthService>();
        }
    }
}
