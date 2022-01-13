using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace Sitko.Core.App.Web;

public interface IWebApplicationModule : IApplicationModule
{
    void ConfigureEndpoints(IConfiguration configuration, IAppEnvironment environment,
        IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
    {
    }

    void ConfigureAppBuilder(IConfiguration configuration, IAppEnvironment environment,
        IApplicationBuilder appBuilder)
    {
    }

    void ConfigureBeforeUseRouting(IConfiguration configuration, IAppEnvironment environment,
        IApplicationBuilder appBuilder)
    {
    }

    void ConfigureAfterUseRouting(IConfiguration configuration, IAppEnvironment environment,
        IApplicationBuilder appBuilder)
    {
    }
}
