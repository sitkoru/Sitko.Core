using AntDesign.ProLayout;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App.Web;
using Sitko.Core.Blazor.AntDesign.Server;

namespace Sitko.Core.Apps.Blazor;

public class Startup : AntBlazorStartup
{
    public Startup(IConfiguration configuration, IHostEnvironment environment) : base(configuration, environment)
    {
    }

    protected override void ConfigureAppServices(IServiceCollection services)
    {
        base.ConfigureAppServices(services);
        services.AddValidatorsFromAssemblyContaining<Startup>();
        services.Configure<ProSettings>(Configuration.GetSection("AntDesignPro"));
    }

    protected override void ConfigureAfterRoutingMiddleware(IApplicationBuilder app)
    {
        base.ConfigureAfterRoutingMiddleware(app);
        app.ConfigureLocalization("ru-RU");
    }
}
