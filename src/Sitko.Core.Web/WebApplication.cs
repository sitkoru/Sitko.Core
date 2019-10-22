using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Web
{
    public class WebApplication : Application
    {
        public WebApplication(string[] args) : base(args)
        {
            GetHostBuilder().ConfigureServices(collection =>
            {
                collection.AddSingleton(typeof(WebApplication), this);
            });
        }

        public WebApplication Run<TStartup>(int port = 0) where TStartup : class
        {
            GetHostBuilder().ConfigureWebHostDefaults(builder =>
                builder.UseStartup<TStartup>().UseUrls($"http://*:{port.ToString()}"));

            GetAppHost().Start();
            return this;
        }

        public WebApplication UseStartup<TStartup>() where TStartup : class
        {
            GetHostBuilder().ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<TStartup>();
            });
            return this;
        }

        public async Task RunAsync<TStartup>() where TStartup : class
        {
            await UseStartup<TStartup>().RunAsync();
        }

        protected List<IWebApplicationModule> GetWebModules()
        {
            return Modules.OfType<IWebApplicationModule>().ToList();
        }

        protected override void ConfigureModule(IApplicationModule module)
        {
            base.ConfigureModule(module);
            if (module is IWebApplicationModule webApplicationModule)
            {
                GetHostBuilder().ConfigureWebHostDefaults(builder =>
                {
                    webApplicationModule.ConfigureWebHostDefaults(builder);
                });
                GetHostBuilder().ConfigureWebHost(builder =>
                {
                    webApplicationModule.ConfigureWebHost(builder);
                });
            }
        }

        protected override void ConfigureModule<TModuleConfig>(IApplicationModule<TModuleConfig> module,
            Func<IConfiguration, IHostEnvironment, TModuleConfig> configure)
        {
            base.ConfigureModule(module, configure);
            if (module is IWebApplicationModule webApplicationModule)
            {
                GetHostBuilder().ConfigureWebHostDefaults(builder =>
                {
                    webApplicationModule.ConfigureWebHostDefaults(builder);
                });
                GetHostBuilder().ConfigureWebHost(builder =>
                {
                    webApplicationModule.ConfigureWebHost(builder);
                });
            }
        }

        public void BeforeRoutingHook(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            foreach (var webModule in GetWebModules())
            {
                webModule.ConfigureBeforeUseRouting(configuration, environment, appBuilder);
            }
        }

        public void AfterRoutingHook(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            foreach (var webModule in GetWebModules())
            {
                webModule.ConfigureAfterUseRouting(configuration, environment, appBuilder);
            }
        }

        public void EndpointsHook(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
        {
            foreach (var webModule in GetWebModules())
            {
                webModule.ConfigureEndpoints(configuration, environment, appBuilder, endpoints);
            }
        }
    }
}
