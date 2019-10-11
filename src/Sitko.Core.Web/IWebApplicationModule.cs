using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Sitko.Core.App;

namespace Sitko.Core.Web
{
    public interface IWebApplicationModule : IApplicationModule
    {
        Task ApplicationStarted(IApplicationBuilder appBuilder);
        Task ApplicationStopping(IApplicationBuilder appBuilder);
        Task ApplicationStopped(IApplicationBuilder appBuilder);
        void ConfigureEndpoints(IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints);
        void ConfigureBeforeUseRouting(IApplicationBuilder appBuilder);
        void ConfigureAfterUseRouting(IApplicationBuilder appBuilder);
        void ConfigureWebHostDefaults(IWebHostBuilder webHostBuilder);
        void ConfigureWebHost(IWebHostBuilder webHostBuilder);
    }
}
