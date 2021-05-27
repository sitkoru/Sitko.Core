using System;
using System.Collections.Concurrent;
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
using Sitko.Core.App.Localization;
using Sitko.Core.App.Logging;
using Tempus;

namespace Sitko.Core.App
{
    public abstract class Application : IApplication, IAsyncDisposable
    {
        private readonly string[] _args;
        public readonly Guid Id = Guid.NewGuid();
        public string Name { get; private set; } = "App";
        public string Version { get; private set; } = "dev";

        private static readonly ConcurrentDictionary<Guid, Application> _apps = new();

        private readonly bool _check;

        private readonly List<Action<LoggerConfiguration, LogLevelSwitcher>> _loggerConfigurationActions = new();
        private readonly List<Action<HostBuilderContext, IServiceCollection>> _servicesConfigurationActions = new();
        private readonly List<Action<HostBuilderContext, IConfigurationBuilder>> _appConfigurationActions = new();

        private readonly Dictionary<string, object> _store = new();

        protected readonly Dictionary<string, LogEventLevel> LogEventLevels = new();

        private readonly Dictionary<Type, ApplicationModuleRegistration> _moduleRegistrations =
            new();

        private IHost? _appHost;

        protected Application(string[] args)
        {
            _args = args;
            _apps.TryAdd(Id, this);
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length > 0 && args[0] == "check")
            {
                _check = true;
            }
        }

        public static Application GetApp(Guid id)
        {
            if (_apps.ContainsKey(id))
            {
                return _apps[id];
            }

            throw new ArgumentException($"Application {id} is not registered", nameof(id));
        }

        protected IHost Build(Action<IHostBuilder>? configure = null)
        {
            if (_appHost is not null)
            {
                return _appHost;
            }

            var logLevelSwitcher = new LogLevelSwitcher();

            using var tmpHost = CreateHostBuilder(_args)
                .UseDefaultServiceProvider(options =>
                {
                    options.ValidateOnBuild = false;
                    options.ValidateScopes = true;
                })
                .ConfigureLogging(builder => { builder.SetMinimumLevel(LogLevel.Information); }).Build();

            var tmpConfiguration = tmpHost.Services.GetRequiredService<IConfiguration>();
            var tmpEnvironment = tmpHost.Services.GetRequiredService<IHostEnvironment>();

            var name = GetName();
            Name = !string.IsNullOrEmpty(name) ? name : tmpEnvironment.ApplicationName;

            var version = GetVersion();
            if (!string.IsNullOrEmpty(version))
            {
                Version = version;
            }

            var tmpApplicationContext = new ApplicationContext(Name, Version, tmpEnvironment, tmpConfiguration);

            var tmpLogger = tmpHost.Services.GetRequiredService<ILogger<Application>>();
            tmpLogger.LogInformation("Check required modules");
            var modulesCheckSuccess = true;
            foreach (var registration in _moduleRegistrations)
            {
                var result =
                    registration.Value.CheckRequiredModules(tmpApplicationContext, _moduleRegistrations.Keys.ToArray());
                if (!result.isSuccess)
                {
                    foreach (var missingModule in result.missingModules)
                    {
                        tmpLogger.LogCritical("Required module {MissingModule} for module {Module} is not registered",
                            missingModule, registration.Key);
                    }

                    modulesCheckSuccess = false;
                }
            }

            if (!modulesCheckSuccess)
            {
                tmpLogger.LogInformation("Check required modules failed");
                Environment.Exit(1);
            }


            InitApplication();

            var hostBuilder = CreateHostBuilder(_args)
                .UseDefaultServiceProvider(options =>
                {
                    options.ValidateOnBuild = true;
                    options.ValidateScopes = true;
                })
                .ConfigureAppConfiguration((context, builder) =>
                {
                    foreach (var appConfigurationAction in _appConfigurationActions)
                    {
                        appConfigurationAction(context, builder);
                    }
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton(logLevelSwitcher);
                    services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory());
                    services.AddSingleton(typeof(IApplication), this);
                    services.AddSingleton(typeof(Application), this);
                    services.AddSingleton(GetType(), this);
                    services.AddHostedService<ApplicationLifetimeService>();
                    services.AddTransient<IScheduler, Scheduler>();
                    services.AddTransient(typeof(ILocalizationProvider<>), typeof(LocalizationProvider<>));

                    foreach (var servicesConfigurationAction in _servicesConfigurationActions)
                    {
                        servicesConfigurationAction(context, services);
                    }

                    var appContext = GetContext(context.HostingEnvironment, context.Configuration);
                    foreach (var moduleRegistration in _moduleRegistrations)
                    {
                        moduleRegistration.Value.Configure(appContext, services);
                        moduleRegistration.Value.ConfigureServices(appContext, services);
                    }
                }).ConfigureLogging((context, _) =>
                {
                    var loggerConfiguration = new LoggerConfiguration();
                    loggerConfiguration.MinimumLevel.ControlledBy(logLevelSwitcher.Switch);
                    loggerConfiguration
                        .Enrich.FromLogContext()
                        .Enrich.WithMachineName()
                        .Enrich.WithProperty("App", Name)
                        .Enrich.WithProperty("AppVersion", Version);
                    logLevelSwitcher.Switch.MinimumLevel =
                        context.HostingEnvironment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information;

                    if (LoggingEnableConsole(context))
                    {
                        loggerConfiguration
                            .WriteTo.Console(
                                outputTemplate: ConsoleLogFormat,
                                levelSwitch: logLevelSwitcher.Switch);
                    }

                    ConfigureLogging(loggerConfiguration, logLevelSwitcher);
                    foreach ((var key, LogEventLevel value) in LogEventLevels)
                    {
                        loggerConfiguration.MinimumLevel.Override(key, value);
                    }

                    foreach (var moduleRegistration in _moduleRegistrations)
                    {
                        moduleRegistration.Value.ConfigureLogging(tmpApplicationContext, loggerConfiguration,
                            logLevelSwitcher);
                    }

                    foreach (var loggerConfigurationAction in _loggerConfigurationActions)
                    {
                        loggerConfigurationAction(loggerConfiguration, logLevelSwitcher);
                    }

                    Log.Logger = loggerConfiguration.CreateLogger();
                });


            foreach (var moduleRegistration in _moduleRegistrations)
            {
                moduleRegistration.Value.ConfigureHostBuilder(tmpApplicationContext, hostBuilder);
            }

            configure?.Invoke(hostBuilder);

            IHost? host = null;
            try
            {
                //Init();
                host = hostBuilder.Build();
            }
            catch (Exception e)
            {
                tmpLogger.LogError("Host build error: {ErrorText}", e.ToString());
                Environment.Exit(255);
            }

            if (_check)
            {
                Console.WriteLine("Check run is successful");
                Environment.Exit(0);
            }

            _appHost = host;
            return _appHost;
        }

        protected IApplicationModule[] RegisteredModules =>
            _moduleRegistrations.Values.Select(r => r.GetInstance()).ToArray();

        private IHostBuilder CreateHostBuilder(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);
            ConfigureHostBuilder(builder);
            return builder;
        }

        protected virtual void ConfigureHostBuilder(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(ConfigureHostConfiguration);
            builder.ConfigureAppConfiguration(ConfigureAppConfiguration);
        }

        protected virtual void ConfigureHostConfiguration(IConfigurationBuilder configurationBuilder)
        {
        }

        protected virtual void ConfigureAppConfiguration(HostBuilderContext context,
            IConfigurationBuilder configurationBuilder)
        {
        }

        protected virtual bool LoggingEnableConsole(HostBuilderContext context) =>
            context.HostingEnvironment.IsDevelopment();

        protected virtual void ConfigureLogging(LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher)
        {
        }

        public virtual ValueTask DisposeAsync()
        {
            _appHost?.Dispose();
            return new ValueTask();
        }

        public async Task RunAsync()
        {
            var host = await BuildAndInitAsync();

            await host.RunAsync();
        }

        public async Task<IHost> StartAsync()
        {
            var host = await BuildAndInitAsync();

            await host.StartAsync();
            return host;
        }

        public async Task StopAsync()
        {
            await Build().StopAsync();
        }

        public async Task ExecuteAsync(Func<IServiceProvider, Task> command)
        {
            var host = await BuildAndInitAsync(builder => builder.UseConsoleLifetime());

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
            return Build().Services;
        }

        protected virtual string ConsoleLogFormat =>
            "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}]{NewLine}\t{Message:lj}{NewLine}{Exception}";
        

        protected void RegisterModule<TModule, TModuleConfig>(
            Action<IConfiguration, IHostEnvironment, TModuleConfig>? configure = null, string? configKey = null)
            where TModule : IApplicationModule<TModuleConfig>, new() where TModuleConfig : BaseModuleConfig, new()
        {
            if (_moduleRegistrations.ContainsKey(typeof(TModule)))
            {
                throw new Exception($"Module {typeof(TModule)} already registered");
            }

            _moduleRegistrations.Add(typeof(TModule),
                new ApplicationModuleRegistration<TModule, TModuleConfig>(configure, configKey));
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

        public async Task<IHost> BuildAndInitAsync(Action<IHostBuilder>? configure = null)
        {
            var host = Build(configure);

            using var scope = host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Application>>();
            logger.LogInformation("Init modules");
            foreach (var module in _moduleRegistrations)
            {
                logger.LogInformation("Check module {Module} config", module.Key);
                if (!_check)
                {
                    var checkConfigResult = module.Value.CheckConfig(scope.ServiceProvider);
                    if (!checkConfigResult.isSuccess)
                    {
                        foreach (var error in checkConfigResult.errors)
                        {
                            logger.LogError("Module {Module} config error: {Error}", module.Key, error);
                        }

                        logger.LogError("Module {Module} config check failed", module.Key);
                        Environment.Exit(1);
                    }
                }

                logger.LogInformation("Init module {Module}", module.Key);
                await module.Value.InitAsync(
                    GetContext(scope.ServiceProvider), scope.ServiceProvider);
            }

            return host;
        }

        protected ApplicationContext GetContext(IServiceProvider serviceProvider)
        {
            return GetContext(serviceProvider.GetRequiredService<IHostEnvironment>(),
                serviceProvider.GetRequiredService<IConfiguration>());
        }

        protected ApplicationContext GetContext(IHostEnvironment environment, IConfiguration configuration)
        {
            return new(Name, Version, environment, configuration);
        }

        public async Task OnStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Application>>();
            await OnStartedAsync(configuration, environment, serviceProvider);
            foreach (var moduleRegistration in _moduleRegistrations)
            {
                try
                {
                    await moduleRegistration.Value.ApplicationStarted(configuration, environment, serviceProvider);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error on application started hook in module {Module}: {ErrorText}",
                        moduleRegistration.Key,
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
            var logger = serviceProvider.GetRequiredService<ILogger<Application>>();
            await OnStoppingAsync(configuration, environment, serviceProvider);
            foreach (var moduleRegistration in _moduleRegistrations)
            {
                try
                {
                    await moduleRegistration.Value.ApplicationStopping(configuration, environment, serviceProvider);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error on application stopping hook in module {Module}: {ErrorText}",
                        moduleRegistration.Key,
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
            var logger = serviceProvider.GetRequiredService<ILogger<Application>>();
            await OnStoppedAsync(configuration, environment, serviceProvider);
            foreach (var moduleRegistration in _moduleRegistrations)
            {
                try
                {
                    await moduleRegistration.Value.ApplicationStopped(configuration, environment, serviceProvider);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error on application stopped hook in module {Module}: {ErrorText}",
                        moduleRegistration.Key,
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
            return _moduleRegistrations.ContainsKey(typeof(TModule));
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

        public Application ConfigureLogging(Action<LoggerConfiguration, LogLevelSwitcher> configure)
        {
            _loggerConfigurationActions.Add(configure);
            return this;
        }

        public Application ConfigureServices(Action<HostBuilderContext, IServiceCollection> configure)
        {
            _servicesConfigurationActions.Add(configure);
            return this;
        }

        public Application ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configure)
        {
            _appConfigurationActions.Add(configure);
            return this;
        }

        public Application AddModule<TModule>() where TModule : BaseApplicationModule, new()

        {
            RegisterModule<TModule, BaseApplicationModuleConfig>();
            return this;
        }

        public Application AddModule<TModule, TModuleConfig>(
            Action<IConfiguration, IHostEnvironment, TModuleConfig>? configure = null,
            string? configKey = null)
            where TModule : IApplicationModule<TModuleConfig>, new()
            where TModuleConfig : BaseModuleConfig, new()
        {
            RegisterModule<TModule, TModuleConfig>(configure, configKey);
            return this;
        }
    }

    public static class ApplicationExtensions
    {
        public static TApplication ConfigureServices<TApplication>(this TApplication application,
            Action<HostBuilderContext, IServiceCollection> configure) where TApplication : Application
        {
            application.ConfigureServices(configure);
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
            where TModule : IApplicationModule<TModuleConfig>, new()
            where TModuleConfig : BaseModuleConfig, new()
        {
            application.AddModule<TModule, TModuleConfig>(configure);
            return application;
        }

        public static TApplication AddModule<TApplication, TModule>(this TApplication application)
            where TModule : BaseApplicationModule, new()
            where TApplication : Application
        {
            application.AddModule<TModule, BaseApplicationModuleConfig>();
            return application;
        }

        public static TApplication ConfigureAppConfiguration<TApplication>(this TApplication application,
            Action<HostBuilderContext, IConfigurationBuilder> action)
            where TApplication : Application
        {
            application.ConfigureAppConfiguration(action);
            return application;
        }
    }

    public class ApplicationContext
    {
        public string Name { get; }
        public string Version { get; }
        public IHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public ApplicationContext(string name, string version, IHostEnvironment environment,
            IConfiguration configuration)
        {
            Name = name;
            Version = version;
            Environment = environment;
            Configuration = configuration;
        }
    }
}
