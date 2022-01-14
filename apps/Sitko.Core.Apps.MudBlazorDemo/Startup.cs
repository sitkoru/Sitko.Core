using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Web;
using Sitko.Core.Blazor.MudBlazor.Server;
using Sitko.Core.Blazor.MudBlazorComponents;

namespace Sitko.Core.Apps.MudBlazorDemo;

public class Startup : MudBlazorStartup
{
    public Startup(IConfiguration configuration, IHostEnvironment environment) : base(configuration, environment)
    {
    }

    protected override void ConfigureAppServices(IServiceCollection services)
    {
        base.ConfigureAppServices(services);
        services.AddValidatorsFromAssemblyContaining<Startup>();
        services.Configure<MudLayoutOptions>(Configuration.GetSection("MudLayout"));
    }

    protected override void ConfigureAfterRoutingMiddleware(IApplicationBuilder app)
    {
        base.ConfigureAfterRoutingMiddleware(app);
        app.ConfigureLocalization("ru-RU");
    }
}
