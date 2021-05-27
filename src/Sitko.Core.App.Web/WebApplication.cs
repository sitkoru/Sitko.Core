using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App.Web
{
    public abstract class WebApplication : Application
    {
        protected WebApplication(string[] args) : base(args)
        {
        }

        protected List<IWebApplicationModule> GetWebModules()
        {
            return RegisteredModules.OfType<IWebApplicationModule>().ToList();
        }

        public virtual void AppBuilderHook(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            foreach (var webModule in GetWebModules())
            {
                webModule.ConfigureAppBuilder(configuration, environment, appBuilder);
            }
        }

        public virtual void BeforeRoutingHook(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            foreach (var webModule in GetWebModules())
            {
                webModule.ConfigureBeforeUseRouting(configuration, environment, appBuilder);
            }
        }

        public virtual void AfterRoutingHook(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            foreach (var webModule in GetWebModules())
            {
                webModule.ConfigureAfterUseRouting(configuration, environment, appBuilder);
            }
        }

        public virtual void EndpointsHook(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
        {
            foreach (var webModule in GetWebModules())
            {
                webModule.ConfigureEndpoints(configuration, environment, appBuilder, endpoints);
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
                    webBuilder.UseSetting("ApplicationId", this.Id.ToString());
                    webBuilder.UseStartup<TStartup>();
                    ConfigureWebHostDefaults(webBuilder);
                });
        }

        protected virtual void ConfigureWebHostDefaults(IWebHostBuilder webHostBuilder)
        {
        }

        public WebApplication<TStartup> Run()
        {
            Build().Start();
            return this;
        }

        public WebApplication<TStartup> Run(int port)
        {
            Build(builder =>
                builder.ConfigureWebHostDefaults(
                    webHostBuilder => webHostBuilder.UseUrls($"http://*:{port.ToString()}"))).Start();
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
}
