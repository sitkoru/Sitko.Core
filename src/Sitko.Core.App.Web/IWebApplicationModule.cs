using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Sitko.Core.App.Web;

public interface IWebApplicationModule : IApplicationModule
{
    void ConfigureEndpoints(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
    {
    }

    void ConfigureAppBuilder(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder)
    {
    }

    void ConfigureBeforeUseRouting(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder)
    {
    }

    void ConfigureAfterUseRouting(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder)
    {
    }

    void ConfigureAuthMiddleware(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder)
    {
    }
}

public interface IWebApplicationModule<TModuleOptions> : IWebApplicationModule, IApplicationModule<TModuleOptions>
    where TModuleOptions : class, new()
{
    void ConfigureWebHost(IApplicationContext applicationContext, ConfigureWebHostBuilder webHostBuilder,
        TModuleOptions valueTuple)
    {
    }
}
