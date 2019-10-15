using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App
{
    public class Application
    {
        private readonly string[] _args;
        protected readonly List<IApplicationModule> Modules = new List<IApplicationModule>();
        protected readonly ApplicationStore ApplicationStore = new ApplicationStore();
        private IHost _appHost;
        private IConfiguration _configuration;

        private readonly IHostBuilder _hostBuilder;

        public Application(string[] args)
        {
            _args = args;
            _hostBuilder = Host.CreateDefaultBuilder(args);
            _hostBuilder.ConfigureServices(collection =>
            {
                collection.AddSingleton(typeof(Application), this);
            });
        }

        public Application ConfigureServices(Action<IServiceCollection> conifgure)
        {
            _hostBuilder.ConfigureServices(conifgure);
            return this;
        }

        public Application ConfigureServices(Action<HostBuilderContext, IServiceCollection> conifgure)
        {
            _hostBuilder.ConfigureServices(conifgure);
            return this;
        }

        protected IConfiguration GetConfiguration()
        {
            if (_configuration == null)
            {
                _configuration = Host.CreateDefaultBuilder(_args).Build().Services.GetService<IConfiguration>();
            }

            return _configuration;
        }

        public async Task RunAsync()
        {
            await InitAsync();

            await GetAppHost().RunAsync();
        }

        public async Task ExecuteAsync(Func<IServiceProvider, Task> command)
        {
            var host = GetAppHost();

            await InitAsync();

            await host.StartAsync();

            var serviceProvider = host.Services;

            using (var scope = serviceProvider.CreateScope())
            {
                await command(scope.ServiceProvider);
            }

            await host.StopAsync();
        }

        public IServiceProvider GetServices()
        {
            return GetAppHost().Services;
        }

        protected IHost GetAppHost()
        {
            return _appHost ??= _hostBuilder.Build();
        }

        public IHostBuilder GetHostBuilder()
        {
            return _hostBuilder;
        }

        public async Task InitAsync()
        {
            var host = GetAppHost();
            using (var scope = host.Services.CreateScope())
            {
                foreach (var module in Modules)
                {
                    CheckRequiredModules(module);
                    await module.InitAsync(scope.ServiceProvider,
                        scope.ServiceProvider.GetRequiredService<IConfiguration>(),
                        scope.ServiceProvider.GetRequiredService<IHostEnvironment>());
                }
            }
        }

        public Application AddModule<TModule, TModuleConfig>(
            Func<IConfiguration, IHostEnvironment, TModuleConfig> configure)
            where TModule : IApplicationModule<TModuleConfig>, new() where TModuleConfig : class
        {
            if (Modules.OfType<TModule>().Any())
            {
                return this;
            }

            var module = new TModule();
            ConfigureModule(module, configure);
            Modules.Add(module);
            return this;
        }

        public Application AddModule<TModule>()
            where TModule : IApplicationModule, new()
        {
            if (Modules.OfType<TModule>().Any())
            {
                return this;
            }

            var module = new TModule();
            ConfigureModule(module);
            Modules.Add(module);
            return this;
        }

        private void CheckRequiredModules(IApplicationModule module)
        {
            var requiredModules = module.GetRequiredModules();
            foreach (Type requiredModule in requiredModules)
            {
                if (Modules.All(m => m.GetType() != requiredModule))
                {
                    throw new Exception($"Module {module} require module {requiredModule} to be included");
                }
            }
        }


        private void ConfigureModule(IApplicationModule module, IServiceCollection collection,
            IHostEnvironment environment, IConfiguration configuration)
        {
            module.ConfigureServices(collection, configuration, environment);
        }

        protected virtual void ConfigureModule(IApplicationModule module)
        {
            module.ApplicationStore = ApplicationStore;

            _hostBuilder.ConfigureServices(
                (context, collection) =>
                {
                    ConfigureModule(module, collection, context.HostingEnvironment, context.Configuration);
                }
            );
        }

        protected virtual void ConfigureModule<TModuleConfig>(IApplicationModule<TModuleConfig> module,
            Func<IConfiguration, IHostEnvironment, TModuleConfig> configure) where TModuleConfig : class
        {
            module.ApplicationStore = ApplicationStore;
            _hostBuilder.ConfigureServices(
                (context, collection) =>
                {
                    if (configure != null)
                    {
                        module.Configure(configure, context.Configuration, context.HostingEnvironment);
                    }

                    collection.AddSingleton(module.GetConfig());
                    ConfigureModule(module, collection, context.HostingEnvironment, context.Configuration);
                }
            );
        }

        public Application ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> action)
        {
            _hostBuilder.ConfigureAppConfiguration(action);
            return this;
        }
    }
}
