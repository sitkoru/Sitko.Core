using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App.Web;

public abstract class WebApplication : HostedApplication
{
    protected WebApplication(string[] args) : base(args)
    {
    }

    protected List<IWebApplicationModule> GetWebModules(IApplicationContext context) =>
        GetEnabledModuleRegistrations<IWebApplicationModule>(context).Select(r => r.GetInstance())
            .OfType<IWebApplicationModule>()
            .ToList();

    public virtual void AppBuilderHook(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder)
    {
        foreach (var webModule in GetWebModules(applicationContext))
        {
            webModule.ConfigureAppBuilder(applicationContext, appBuilder);
        }
    }

    public virtual void BeforeRoutingHook(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder)
    {
        foreach (var webModule in GetWebModules(applicationContext))
        {
            webModule.ConfigureBeforeUseRouting(applicationContext, appBuilder);
        }
    }

    public virtual void AfterRoutingHook(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder)
    {
        foreach (var webModule in GetWebModules(applicationContext))
        {
            webModule.ConfigureAfterUseRouting(applicationContext, appBuilder);
        }
    }

    public virtual void EndpointsHook(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
    {
        foreach (var webModule in GetWebModules(applicationContext))
        {
            webModule.ConfigureEndpoints(applicationContext, appBuilder, endpoints);
        }
    }
}

public abstract class WebApplication<TStartup> : WebApplication where TStartup : BaseStartup
{
    protected WebApplication(string[] args) : base(args)
    {
    }

    protected override void ConfigureAppConfiguration(HostBuilderContext context,
        IConfigurationBuilder configurationBuilder)
    {
        base.ConfigureAppConfiguration(context, configurationBuilder);

        configurationBuilder.AddEnvironmentVariables();
    }

    protected override void ConfigureHostConfiguration(IConfigurationBuilder configurationBuilder)
    {
        base.ConfigureHostConfiguration(configurationBuilder);
        configurationBuilder.AddUserSecrets<TStartup>(true);
        configurationBuilder.AddEnvironmentVariables();
    }

    protected override void ConfigureHostBuilder(IHostBuilder builder)
    {
        base.ConfigureHostBuilder(builder);
        builder
            .ConfigureServices(collection =>
            {
                collection.AddSingleton(typeof(WebApplication), this);
                collection.AddSingleton(typeof(WebApplication<TStartup>), this);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseSetting("ApplicationId", Id.ToString());
                webBuilder.UseStartup<TStartup>();
                ConfigureWebHostDefaults(webBuilder);
            });
    }

    protected virtual void ConfigureWebHostDefaults(IWebHostBuilder webHostBuilder)
    {
    }

    public WebApplication<TStartup> Run()
    {
        CreateAppHost().Start();
        return this;
    }

    public WebApplication<TStartup> Run(int port)
    {
        CreateAppHost(builder =>
            builder.ConfigureWebHostDefaults(
                webHostBuilder => webHostBuilder.UseUrls($"http://*:{port}"))).Start();
        return this;
    }
}

public static class WebApplicationExtensions
{
    public static TWebApplication Run<TWebApplication, TStartup>(this TWebApplication application)
        where TWebApplication : WebApplication<TStartup> where TStartup : BaseStartup
    {
        application.Run();
        return application;
    }

    public static TWebApplication Run<TWebApplication, TStartup>(this TWebApplication application, int port)
        where TStartup : BaseStartup
        where TWebApplication : WebApplication<TStartup>
    {
        application.Run(port);
        return application;
    }
}
