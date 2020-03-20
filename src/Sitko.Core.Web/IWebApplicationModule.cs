using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Web
{
    public interface IWebApplicationModule : IApplicationModule
    {
        void ConfigureEndpoints(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
        {
        }

        void ConfigureAppBuilder(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
        }
        
        void ConfigureBeforeUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
        }

        void ConfigureAfterUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
        }

        void ConfigureStartupServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
        }
    }
}
