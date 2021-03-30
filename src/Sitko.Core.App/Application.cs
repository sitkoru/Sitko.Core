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
using Tempus;

namespace Sitko.Core.App
{
    public abstract class Application : IApplication, IAsyncDisposable
    {
        private readonly bool _check;
        private readonly LoggerConfiguration _loggerConfiguration = new LoggerConfiguration();
        private readonly LogLevelSwitcher _logLevelSwitcher = new LogLevelSwitcher();
        private readonly HashSet<Type> _registeredModules = new HashSet<Type>();
        private readonly Dictionary<string, object> _store = new Dictionary<string, object>();
        protected readonly IConfiguration Configuration;
        protected readonly IHostEnvironment Environment;

        protected readonly IHostBuilder HostBuilder;

        protected readonly Dictionary<string, LogEventLevel> LogEventLevels = new Dictionary<string, LogEventLevel>();
        protected readonly ILogger<Application> Logger;
        protected readonly List<IApplicationModule> Modules = new List<IApplicationModule>();

        private IHost? _appHost;

        protected Application(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length > 0 && args[0] == "check")
            {
                _check = true;
            }

            var tmpHost = CreateHostBuilder(args)
                .UseDefaultServiceProvider(options =>
                {
                    options.ValidateOnBuild = false;
                    options.ValidateScopes = false;
                })
                .ConfigureLogging(builder => { builder.SetMinimumLevel(LogLevel.Information); }).Build();

            Configuration = tmpHost.Services.GetRequiredService<IConfiguration>();
            Environment = tmpHost.Services.GetRequiredService<IHostEnvironment>();
            Logger = tmpHost.Services.GetRequiredService<ILogger<Application>>();

            Logger.LogInformation("Start application {Application} with Environment {Environment}",
                Environment.ApplicationName, Environment.EnvironmentName);

            Name = Environment.ApplicationName;

            HostBuilder = CreateHostBuilder(args)
                .UseDefaultServiceProvider(options =>
                {
                    options.ValidateOnBuild = true;
                    options.ValidateScopes = true;
                }).ConfigureServices(services =>
                {
                    services.AddSingleton(_logLevelSwitcher);
                    services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory());
                    services.AddSingleton(typeof(IApplication), this);
                    services.AddSingleton(typeof(Application), this);
                    services.AddSingleton(GetType(), this);
                    services.AddHostedService<ApplicationLifetimeService>();
                    services.AddTransient<IScheduler, Scheduler>();
                });
        }

        private IHostBuilder CreateHostBuilder(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);
            ConfigureHostBuilder(builder);
            return builder;
        }

        protected virtual void ConfigureHostBuilder(IHostBuilder builder)
        {
        }

        protected virtual bool LoggingEnableConsole => Environment.IsDevelopment();

        protected virtual void ConfigureLogging(LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher)
        {
        }

        public string Name { get; private set; }
        public string Version { get; private set; } = "dev";

        public virtual ValueTask DisposeAsync()
        {
            _appHost?.Dispose();
            return new ValueTask();
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
                var logger = serviceProvider.GetService<ILogger<Application>>();
                logger.LogError(ex, "Error: {ErrorText}", ex.ToString());
            }
        }

        public IServiceProvider GetServices()
        {
            return GetAppHost().Services;
        }

        protected IHost GetAppHost()
        {
            if (_appHost == null)
            {
                try
                {
                    Init();
                    _appHost = BuildAppHost();
                    Logger.LogInformation("Check required modules");
                    foreach (var module in Modules)
                    {
                        CheckRequiredModules(module);
                    }

                    if (_check)
                    {
                        Console.WriteLine("Check run is successful");
                        System.Environment.Exit(0);
                    }

                    Log.Logger = _loggerConfiguration.CreateLogger();
                }
                catch (Exception e)
                {
                    Logger.LogError("Host build error: {ErrorText}", e.ToString());
                    System.Environment.Exit(255);
                }
            }

            return _appHost!;
        }

        protected virtual IHost BuildAppHost()
        {
            return HostBuilder.Build();
        }

        public IHostBuilder GetHostBuilder()
        {
            return HostBuilder;
        }

        protected virtual string ConsoleLogFormat =>
            "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}]{NewLine}\t{Message:lj}{NewLine}{Exception}";

        private void InitLogging()
        {
            _loggerConfiguration.MinimumLevel.ControlledBy(_logLevelSwitcher.Switch);
            _loggerConfiguration
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("App", Name)
                .Enrich.WithProperty("AppVersion", Version);
            _logLevelSwitcher.Switch.MinimumLevel =
                Environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information;

            if (LoggingEnableConsole)
            {
                _loggerConfiguration
                    .WriteTo.Console(
                        outputTemplate: ConsoleLogFormat,
                        levelSwitch: _logLevelSwitcher.Switch);
            }

            ConfigureLogging(_loggerConfiguration, _logLevelSwitcher);
            foreach ((var key, LogEventLevel value) in LogEventLevels)
            {
                _loggerConfiguration.MinimumLevel.Override(key, value);
            }
        }


        protected void RegisterModule<TModule, TModuleConfig>(
            Action<IConfiguration, IHostEnvironment, TModuleConfig>? configure = null)
            where TModule : IApplicationModule<TModuleConfig> where TModuleConfig : class, new()
        {
            if (_registeredModules.Contains(typeof(TModule)))
            {
                throw new Exception($"Module {typeof(TModule)} already registered");
            }

            _registeredModules.Add(typeof(TModule));
            var hostBuilderConfig = new TModuleConfig();
            if (!_check)
            {
                configure?.Invoke(Configuration, Environment, hostBuilderConfig);
            }

            var instance = Activator.CreateInstance(typeof(TModule), hostBuilderConfig, this);
            if (instance is IHostBuilderModule<TModuleConfig> hostBuilderModule)
            {
                hostBuilderModule.ConfigureHostBuilder(HostBuilder, Configuration, Environment);
            }

            HostBuilder.ConfigureServices((context, services) =>
            {
                var config = new TModuleConfig();
                if (!_check)
                {
                    configure?.Invoke(context.Configuration, context.HostingEnvironment, config);
                }

                instance = Activator.CreateInstance(typeof(TModule), config, this);
                if (instance is TModule module)
                {
                    if (!_check)
                    {
                        module.CheckConfig();
                    }

                    module.ConfigureLogging(_loggerConfiguration, _logLevelSwitcher,
                        context.Configuration, context.HostingEnvironment);
                    module.ConfigureServices(services, context.Configuration, context.HostingEnvironment);
                    Modules.Add(module);
                }
                else
                {
                    throw new Exception($"Can't instantiate module {typeof(TModule)}");
                }
            });
        }

        private bool _initComplete;

        private void Init()
        {
            if (!_initComplete)
            {
                var name = GetName();
                if (!string.IsNullOrEmpty(name))
                {
                    Name = name;
                }

                var version = GetVersion();
                if (!string.IsNullOrEmpty(version))
                {
                    Version = version;
                }

                InitApplication();
                InitLogging();
                _initComplete = true;
            }
        }

        protected virtual string? GetName()
        {
            return null;
        }

        protected virtual string? GetVersion()
        {
            return GetType().Assembly.GetName().Version?.ToString();
        }

        protected virtual void InitApplication()
        {
        }

        // protected virtual void AddModule(IApplicationModule module)
        // {
        //     Modules.Add(module);
        // }

        public async Task InitAsync()
        {
            var host = GetAppHost();

            using var scope = host.Services.CreateScope();
            Logger.LogInformation("Init modules");
            foreach (var module in Modules)
            {
                Logger.LogInformation("Init module {Module}", module);
                await module.InitAsync(scope.ServiceProvider,
                    scope.ServiceProvider.GetRequiredService<IConfiguration>(),
                    scope.ServiceProvider.GetRequiredService<IHostEnvironment>());
            }
        }

        private void CheckRequiredModules(IApplicationModule module)
        {
            var requiredModules = module.GetRequiredModules();
            foreach (var requiredModule in requiredModules.Where(requiredModule =>
                Modules.All(m => !requiredModule.IsInstanceOfType(m))))
            {
                throw new Exception($"Module {module} require module {requiredModule} to be included");
            }
        }

        public async Task OnStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            await OnStartedAsync(configuration, environment, serviceProvider);
            foreach (var module in Modules)
            {
                try
                {
                    await module.ApplicationStarted(configuration, environment, serviceProvider);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error on application started hook in module {Module}: {ErrorText}", module,
                        ex.ToString());
                }
            }
        }

        protected virtual Task OnStartedAsync(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        public async Task OnStopping(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            await OnStoppingAsync(configuration, environment, serviceProvider);
            foreach (var module in Modules)
            {
                try
                {
                    await module.ApplicationStopping(configuration, environment, serviceProvider);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error on application stopping hook in module {Module}: {ErrorText}", module,
                        ex.ToString());
                }
            }
        }

        protected virtual Task OnStoppingAsync(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        public async Task OnStopped(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            await OnStoppedAsync(configuration, environment, serviceProvider);
            foreach (var module in Modules)
            {
                try
                {
                    await module.ApplicationStopped(configuration, environment, serviceProvider);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error on application stopped hook in module {Module}: {ErrorText}", module,
                        ex.ToString());
                }
            }
        }

        protected virtual Task OnStoppedAsync(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        public bool HasModule<TModule>() where TModule : IApplicationModule
        {
            return Modules.OfType<TModule>().Any();
        }


        public void Set(string key, object value)
        {
            _store[key] = value;
        }

        public T Get<T>(string key)
        {
            if (_store.ContainsKey(key))
            {
                return (T)_store[key];
            }

#pragma warning disable 8603
            return default;
#pragma warning restore 8603
        }

        public Application ConfigureLogLevel(string source, LogEventLevel level)
        {
            LogEventLevels.Add(source, level);
            return this;
        }

        public Application AddModule<TModule, TModuleConfig>(
            Action<IConfiguration, IHostEnvironment, TModuleConfig>? configure = null)
            where TModule : IApplicationModule<TModuleConfig>
            where TModuleConfig : class, new()
        {
            RegisterModule<TModule, TModuleConfig>(configure);
            return this;
        }
    }

    public static class ApplicationExtensions
    {
        public static T ConfigureServices<T>(this T application, Action<IServiceCollection> configure)
            where T : Application
        {
            application.GetHostBuilder().ConfigureServices(configure);
            return application;
        }

        public static TApplication ConfigureServices<TApplication>(this TApplication application,
            Action<HostBuilderContext, IServiceCollection> configure) where TApplication : Application
        {
            application.GetHostBuilder().ConfigureServices(configure);
            return application;
        }

        public static TApplication ConfigureLogLevel<TApplication>(this TApplication application, string source,
            LogEventLevel level) where TApplication : Application
        {
            application.ConfigureLogLevel(source, level);
            return application;
        }

        public static TApplication AddModule<TApplication, TModule, TModuleConfig>(this TApplication application,
            Action<IConfiguration, IHostEnvironment, TModuleConfig>? configure = null)
            where TApplication : Application
            where TModule : IApplicationModule<TModuleConfig>
            where TModuleConfig : class, new()
        {
            application.AddModule<TModule, TModuleConfig>(configure);
            return application;
        }

        public static TApplication AddModule<TApplication, TModule>(this TApplication application)
            where TModule : BaseApplicationModule
            where TApplication : Application
        {
            application.AddModule<TModule, BaseApplicationModuleConfig>();
            return application;
        }

        public static TApplication ConfigureAppConfiguration<TApplication>(this TApplication application,
            Action<HostBuilderContext, IConfigurationBuilder> action)
            where TApplication : Application
        {
            application.GetHostBuilder().ConfigureAppConfiguration(action);
            return application;
        }
    }
}
