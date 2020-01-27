using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Web;

namespace Sitko.Core.Grpc.Server
{
    public class GrpcServiceModule<TService> : BaseApplicationModule<GrpcServerOptions>, IWebApplicationModule
        where TService : class
    {
        public override async Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            var registrar = serviceProvider.GetRequiredService<GrpcServicesRegistrar>();
            await registrar.RegisterAsync<TService>();
        }

        public void ConfigureEndpoints(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGrpcService<TService>();
        }

        public override List<Type> GetRequiredModules()
        {
            return new List<Type> {typeof(GrpcServerModule)};
        }
    }
}
