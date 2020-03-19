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
using Microsoft.Extensions.Logging;
using Sitko.Core.App;

namespace Sitko.Core.Web
{
    public abstract class WebApplication<T> : Application<T> where T : WebApplication<T>
    {
        private static T? _instance;

        protected WebApplication(string[] args) : base(args)
        {
            GetHostBuilder().ConfigureServices(collection =>
            {
                collection.AddSingleton(typeof(WebApplication<T>), this);
            });
            _instance = (T)this;
        }

        public static T GetInstance()
        {
            return _instance!;
        }

        public T Run<TStartup>(int port = 0) where TStartup : BaseStartup<T>
        {
            GetHostBuilder().ConfigureWebHostDefaults(builder =>
                builder.UseStartup<TStartup>().UseUrls($"http://*:{port.ToString()}"));

            GetAppHost().Start();
            return (T)this;
        }

        public T UseStartup<TStartup>() where TStartup : BaseStartup<T>
        {
            GetHostBuilder().ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<TStartup>();
            });
            return (T)this;
        }

        public async Task RunAsync<TStartup>() where TStartup : BaseStartup<T>
        {
            await UseStartup<TStartup>().RunAsync();
        }

        public async Task ExecuteAsync<TStartup>(Func<IServiceProvider, Task> command) where TStartup : BaseStartup<T>
        {
            GetHostBuilder().UseConsoleLifetime();
            using var host = UseStartup<TStartup>().GetAppHost();
            await InitAsync();

            var serviceProvider = host.Services;
            await host.StartAsync();
            try
            {
                using var scope = serviceProvider.CreateScope();
                await command(scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetService<ILogger<WebApplication<T>>>();
                logger.LogError(ex, ex.ToString());
            }

            await host.StopAsync();
        }

        protected List<IWebApplicationModule> GetWebModules()
        {
            return Modules.OfType<IWebApplicationModule>().ToList();
        }

        protected override void AddModule(IApplicationModule module)
        {
            base.AddModule(module);
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

        public virtual void ConfigureStartupServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            foreach (var webModule in GetWebModules())
            {
                webModule.ConfigureStartupServices(services, configuration, environment);
            }
        }

        public IHostBuilder CreateBasicHostBuilder<TStartup>()
            where TStartup : BaseStartup<T>
        {
            return GetHostBuilder().ConfigureAppConfiguration(builder =>
            {
                builder.AddUserSecrets<TStartup>();
                builder.AddEnvironmentVariables();
            }).ConfigureWebHost(builder => builder.UseStartup<TStartup>());
        }
    }
}
