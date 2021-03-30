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
        private static WebApplication? _instance;

        protected WebApplication(string[] args) : base(args)
        {
            _instance = this;
        }

        public static WebApplication GetInstance()
        {
            return _instance!;
        }

        public virtual void ConfigureStartupServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            foreach (var webModule in GetWebModules())
            {
                webModule.ConfigureStartupServices(services, configuration, environment);
            }
        }

        protected List<IWebApplicationModule> GetWebModules()
        {
            return Modules.OfType<IWebApplicationModule>().ToList();
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
            GetHostBuilder()
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.UseStartup<TStartup>();
                    ConfigureWebHostDefaults(builder);
                })
                .ConfigureServices(collection =>
                {
                    collection.AddSingleton(typeof(WebApplication<TStartup>), this);
                });
        }

        protected virtual void ConfigureWebHostDefaults(IWebHostBuilder webHostBuilder)
        {
        }


        public WebApplication<TStartup> Run()
        {
            GetAppHost().Start();
            return this;
        }

        public WebApplication<TStartup> Run(int port)
        {
            GetHostBuilder().ConfigureWebHostDefaults(builder =>
                builder.UseStartup<TStartup>().UseUrls($"http://*:{port.ToString()}"));

            GetAppHost().Start();
            return this;
        }


        public IHostBuilder CreateBasicHostBuilder()
        {
            return GetHostBuilder().ConfigureAppConfiguration(builder =>
            {
                builder.AddUserSecrets<TStartup>();
                builder.AddEnvironmentVariables();
            }).ConfigureWebHostDefaults(builder => builder.UseStartup<TStartup>());
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
