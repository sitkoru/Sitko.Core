using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Web
{
    public interface IWebApplicationModule : IApplicationModule
    {
        Task ApplicationStarted(IConfiguration configuration, IHostEnvironment environment, IApplicationBuilder appBuilder);
        Task ApplicationStopping(IConfiguration configuration, IHostEnvironment environment, IApplicationBuilder appBuilder);
        Task ApplicationStopped(IConfiguration configuration, IHostEnvironment environment, IApplicationBuilder appBuilder);
        void ConfigureEndpoints(IConfiguration configuration, IHostEnvironment environment, IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints);
        void ConfigureBeforeUseRouting(IConfiguration configuration, IHostEnvironment environment, IApplicationBuilder appBuilder);
        void ConfigureAfterUseRouting(IConfiguration configuration, IHostEnvironment environment, IApplicationBuilder appBuilder);
        void ConfigureWebHostDefaults(IWebHostBuilder webHostBuilder);
        void ConfigureWebHost(IWebHostBuilder webHostBuilder);
    }
}
