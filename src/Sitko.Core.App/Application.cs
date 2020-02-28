using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Sitko.Core.App.Logging;

namespace Sitko.Core.App
{
    public class Application<T> : IAsyncDisposable where T : Application<T>
    {
        protected readonly List<IApplicationModule> Modules = new List<IApplicationModule>();
        protected readonly ApplicationStore ApplicationStore = new ApplicationStore();
        private IHost? _appHost;
        public readonly IConfiguration Configuration;
        public readonly IHostEnvironment Environment;

        private readonly IHostBuilder _hostBuilder;
        private readonly LoggerConfiguration _loggerConfiguration = new LoggerConfiguration();
        private readonly LogLevelSwitcher _logLevelSwitcher = new LogLevelSwitcher();

        protected LogEventLevel LoggingProductionLevel { get; set; } = LogEventLevel.Information;
        protected LogEventLevel LoggingDevelopmentLevel { get; set; } = LogEventLevel.Debug;
        protected bool LoggingEnableConsole { get; set; } = false;
        protected string LoggingFacility { get; set; }
        protected Action<LoggerConfiguration, LogLevelSwitcher>? LoggingConfigure { get; set; }

        private readonly Dictionary<string, LogEventLevel> _logEventLevels = new Dictionary<string, LogEventLevel>();

        public Application(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var tmpHost = Host.CreateDefaultBuilder(args).ConfigureHostConfiguration(builder =>
            {
                builder.AddEnvironmentVariables("ASPNETCORE_");
            }).Build();

            Configuration = tmpHost.Services.GetService<IConfiguration>();
            Environment = tmpHost.Services.GetService<IHostEnvironment>();
            LoggingFacility ??= Environment.ApplicationName;
            LoggingEnableConsole = Environment.IsDevelopment();

            _loggerConfiguration.MinimumLevel.ControlledBy(_logLevelSwitcher.Switch);
            _loggerConfiguration.Enrich.FromLogContext()
                .Enrich.WithProperty("App", LoggingFacility);

            if (Environment.IsDevelopment())
            {
                _logLevelSwitcher.Switch.MinimumLevel = LoggingDevelopmentLevel;
                _logLevelSwitcher.MsMessagesSwitch.MinimumLevel = LoggingDevelopmentLevel;
            }
            else
            {
                _logLevelSwitcher.Switch.MinimumLevel = LoggingProductionLevel;
                _logLevelSwitcher.MsMessagesSwitch.MinimumLevel = LogEventLevel.Warning;
                _loggerConfiguration.MinimumLevel.Override("Microsoft", _logLevelSwitcher.MsMessagesSwitch);
            }

            _hostBuilder = Host.CreateDefaultBuilder(args);
            _hostBuilder.ConfigureServices((context, services) =>
            {
                services.AddSingleton(typeof(Application<T>), this);
                services.AddSingleton(typeof(T), this);
                services.AddHostedService<ApplicationLifetimeService<T>>();
                services.AddSingleton(_logLevelSwitcher);
                services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory());
            });
        }

        public T ConfigureServices(Action<IServiceCollection> configure)
        {
            _hostBuilder.ConfigureServices(configure);
            return (T)this;
        }

        public T ConfigureServices(Action<HostBuilderContext, IServiceCollection> configure)
        {
            _hostBuilder.ConfigureServices(configure);
            return (T)this;
        }

        public async Task RunAsync()
        {
            await InitAsync();

            await GetAppHost().RunAsync();
        }

        public async Task StartAsync()
        {
            await InitAsync();

            await GetAppHost().StartAsync();
        }

        public async Task StopAsync()
        {
            await GetAppHost().StopAsync();
        }

        public async Task ExecuteAsync(Func<IServiceProvider, Task> command)
        {
            GetHostBuilder().UseConsoleLifetime();
            var host = GetAppHost();

            await InitAsync();

            var serviceProvider = host.Services;

            try
            {
                using var scope = serviceProvider.CreateScope();
                await command(scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetService<ILogger<Application<T>>>();
                logger.LogError(ex, ex.ToString());
            }
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

        public T ConfigureLogLevel(string source, LogEventLevel level)
        {
            _logEventLevels.Add(source, level);
            return (T)this;
        }

        protected virtual void ConfigureLogging()
        {
            if (LoggingEnableConsole)
            {
                _loggerConfiguration
                    .WriteTo.Console(
                        outputTemplate:
                        "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}",
                        levelSwitch: _logLevelSwitcher.Switch);
            }

            LoggingConfigure?.Invoke(_loggerConfiguration, _logLevelSwitcher);
            foreach (var entry in _logEventLevels)
            {
                _loggerConfiguration.MinimumLevel.Override(entry.Key, entry.Value);
            }
        }

        public async Task InitAsync()
        {
            var host = GetAppHost();
            ConfigureLogging();
            Log.Logger = _loggerConfiguration.CreateLogger();

            using var scope = host.Services.CreateScope();
            foreach (var module in Modules)
            {
                CheckRequiredModules(module);
                await module.InitAsync(scope.ServiceProvider,
                    scope.ServiceProvider.GetRequiredService<IConfiguration>(),
                    scope.ServiceProvider.GetRequiredService<IHostEnvironment>());
            }
        }

        public T AddModule<TModule, TModuleConfig>(
            Func<IConfiguration, IHostEnvironment, TModuleConfig> configure)
            where TModule : IApplicationModule<TModuleConfig>, new() where TModuleConfig : class
        {
            if (Modules.OfType<TModule>().Any())
            {
                return (T)this;
            }

            var module = new TModule();
            ConfigureModule(module, configure);
            Modules.Add(module);
            return (T)this;
        }

        public T AddModule<TModule>() where TModule : BaseApplicationModule, new()
        {
            AddModule<TModule, BaseApplicationModuleConfig>((configuration, environment) =>
                new BaseApplicationModuleConfig());
            return (T)this;
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
                    module.ConfigureLogging(_loggerConfiguration, _logLevelSwitcher, LoggingFacility,
                        context.Configuration, context.HostingEnvironment);
                    module.ConfigureServices(collection, context.Configuration, context.HostingEnvironment);
                }
            );
        }

        public T ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> action)
        {
            _hostBuilder.ConfigureAppConfiguration(action);
            return (T)this;
        }

        public void OnStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            foreach (var module in Modules)
            {
                module.ApplicationStarted(configuration, environment, serviceProvider);
            }
        }

        public void OnStopping(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            foreach (var module in Modules)
            {
                module.ApplicationStopping(configuration, environment, serviceProvider);
            }
        }

        public void OnStopped(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            foreach (var module in Modules)
            {
                module.ApplicationStopped(configuration, environment, serviceProvider);
            }
        }

        public virtual ValueTask DisposeAsync()
        {
            _appHost.Dispose();
            return new ValueTask();
        }
    }
}
