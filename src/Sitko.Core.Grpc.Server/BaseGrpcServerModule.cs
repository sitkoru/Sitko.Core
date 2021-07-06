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
        IWebApplicationModule where TConfig : GrpcServerModuleOptions, new()
    {
        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TConfig startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddGrpc(options =>
            {
                options.EnableDetailedErrors = startupOptions.EnableDetailedErrors;
                startupOptions.Configure?.Invoke(options);
            });
            if (startupOptions.EnableReflection)
            {
                services.AddGrpcReflection();
            }

            foreach (var registration in startupOptions.ServiceRegistrations)
            {
                registration(this);
            }
        }

        public void ConfigureEndpoints(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
        {
            var config = GetOptions(appBuilder.ApplicationServices);
            foreach (var endpointRegistration in _endpointRegistrations)
            {
                endpointRegistration(endpoints);
            }

            endpoints.MapGrpcService<HealthService>();
            if (config.EnableReflection)
            {
                endpoints.MapGrpcReflectionService();
            }
        }

        private readonly List<Action<IEndpointRouteBuilder>> _endpointRegistrations = new();

        public virtual void RegisterService<TService>() where TService : class
        {
            _endpointRegistrations.Add(builder => builder.MapGrpcService<TService>());
        }
    }
}
