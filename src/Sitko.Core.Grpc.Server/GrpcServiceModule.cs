using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Web;

namespace Sitko.Core.Grpc.Server
{
    public class GrpcServiceModule<TService> : BaseApplicationModule<GrpcServerOptions>, IWebApplicationModule
        where TService : class
    {
        public async Task ApplicationStarted(IApplicationBuilder appBuilder)
        {
            var registrar =
                appBuilder.ApplicationServices.GetRequiredService<GrpcServicesRegistrar>();
            await registrar.RegisterAsync<TService>();
        }

        public async Task ApplicationStopping(IApplicationBuilder appBuilder)
        {
            var registrar =
                appBuilder.ApplicationServices.GetRequiredService<GrpcServicesRegistrar>();
            await registrar.StopAsync<TService>();
        }

        public Task ApplicationStopped(IApplicationBuilder appBuilder)
        {
            return Task.CompletedTask;
        }

        public void ConfigureEndpoints(IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGrpcService<TService>();
        }

        public void ConfigureBeforeUseRouting(IApplicationBuilder appBuilder)
        {
        }

        public void ConfigureAfterUseRouting(IApplicationBuilder appBuilder)
        {
        }

        public void ConfigureWebHostDefaults(IWebHostBuilder webHostBuilder)
        {
        }

        public void ConfigureWebHost(IWebHostBuilder webHostBuilder)
        {
        }

        public override List<Type> GetRequiredModules()
        {
            return new List<Type> {typeof(GrpcServerModule)};
        }
    }
}
