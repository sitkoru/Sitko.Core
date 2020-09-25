using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Grpc.Server
{
    public abstract class BaseGrpcServerModule<TConfig> : BaseApplicationModule<TConfig>, IGrpcServerModule,
        IWebApplicationModule where TConfig : GrpcServerOptions, new()
    {
        protected BaseGrpcServerModule(TConfig config, Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddGrpc(options =>
            {
                options.EnableDetailedErrors = Config.EnableDetailedErrors;
                Config.Configure?.Invoke(options);
            });
            if (Config.EnableReflection)
            {
                services.AddGrpcReflection();
            }

            foreach (var registration in Config.ServiceRegistrations)
            {
                registration(this);
            }
        }

        public void ConfigureEndpoints(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
        {
            foreach (var endpointRegistration in _endpointRegistrations)
            {
                endpointRegistration(endpoints);
            }

            endpoints.MapGrpcService<HealthService>();
            if (Config.EnableReflection)
            {
                endpoints.MapGrpcReflectionService();
            }
        }

        private readonly List<Action<IEndpointRouteBuilder>> _endpointRegistrations =
            new List<Action<IEndpointRouteBuilder>>();

        public virtual void RegisterService<TService>() where TService : class
        {
            _endpointRegistrations.Add(builder => builder.MapGrpcService<TService>());
        }
    }
}
