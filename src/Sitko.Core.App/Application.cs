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
using Sitko.Core.App.Logging;
using Tempus;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Sitko.Core.App
{
    public abstract class Application : IApplication, IAsyncDisposable
    {
        private readonly string[] _args;
        public readonly Guid Id = Guid.NewGuid();
        public string Name { get; private set; } = "App";
        public string Version { get; private set; } = "dev";

        private static readonly ConcurrentDictionary<Guid, Application> _apps = new();

        public readonly bool IsPostBuildCheckRun;

        private readonly List<Action<ApplicationContext, LoggerConfiguration, LogLevelSwitcher>>
            _loggerConfigurationActions = new();

        private readonly List<Action<ApplicationContext, HostBuilderContext, IServiceCollection>>
            _servicesConfigurationActions = new();

        private readonly List<Action<ApplicationContext, HostBuilderContext, IConfigurationBuilder>>
            _appConfigurationActions = new();

        private readonly Dictionary<string, object> _store = new();

        protected readonly Dictionary<string, LogEventLevel> LogEventLevels = new();

        private readonly Dictionary<Type, ApplicationModuleRegistration> _moduleRegistrations =
            new();

        private IHost? _appHost;

        private static readonly string BaseConsoleLogFormat =
            "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}]{NewLine}\t{Message:lj}{NewLine}{Exception}";

        protected virtual string ConsoleLogFormat => BaseConsoleLogFormat;

        private ILogger<Application> InternalLogger { get; set; }

        protected Application(string[] args)
        {
            _args = args;
            _apps.TryAdd(Id, this);
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length > 0 && args[0] == "check")
            {
                IsPostBuildCheckRun = true;
            }

            var loggerConfiguration = new LoggerConfiguration();
            loggerConfiguration
                .WriteTo.Console(outputTemplate: BaseConsoleLogFormat,
                    restrictedToMinimumLevel: LogEventLevel.Debug);
            InternalLogger = new SerilogLoggerFactory(loggerConfiguration.CreateLogger()).CreateLogger<Application>();
        }

        public static Application GetApp(Guid id)
        {
            if (_apps.ContainsKey(id))
            {
                return _apps[id];
            }

            throw new ArgumentException($"Application {id} is not registered", nameof(id));
        }

        private IHostBuilder PrepareHostBuilder(Action<IHostBuilder>? configure = null)
        {
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

            var tmpLogger = tmpHost.Services.GetRequiredService<ILogger<Application>>();
            var tmpApplicationContext = GetContext(tmpEnvironment, tmpConfiguration);

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
                    var appContext = GetContext(context.HostingEnvironment, context.Configuration);
                    foreach (var appConfigurationAction in _appConfigurationActions)
                    {
                        appConfigurationAction(appContext, context, builder);
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

                    var appContext = GetContext(context.HostingEnvironment, context.Configuration);
                    foreach (var servicesConfigurationAction in _servicesConfigurationActions)
                    {
                        servicesConfigurationAction(appContext, context, services);
                    }

                    foreach (var moduleRegistration in _moduleRegistrations)
                    {
                        moduleRegistration.Value.ConfigureOptions(appContext, services);
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

                    var appContext = GetContext(context.HostingEnvironment, context.Configuration);
                    ConfigureLogging(appContext,
                        loggerConfiguration,
                        logLevelSwitcher);
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
                        loggerConfigurationAction(appContext, loggerConfiguration, logLevelSwitcher);
                    }

                    Log.Logger = loggerConfiguration.CreateLogger();
                });


            foreach (var moduleRegistration in _moduleRegistrations)
            {
                moduleRegistration.Value.ConfigureHostBuilder(tmpApplicationContext, hostBuilder);
            }

            configure?.Invoke(hostBuilder);

            return hostBuilder;
        }

        protected IHost Build(Action<IHostBuilder>? configure = null)
        {
            if (_appHost is not null)
            {
                return _appHost;
            }

            var hostBuilder = PrepareHostBuilder(configure);

            var host = hostBuilder.Build();

            if (IsPostBuildCheckRun)
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

        protected virtual void ConfigureLogging(ApplicationContext applicationContext,
            LoggerConfiguration loggerConfiguration,
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

        public IHostBuilder GetHostBuilder()
        {
            return PrepareHostBuilder();
        }

        public IServiceProvider GetServices()
        {
            return Build().Services;
        }


        protected void RegisterModule<TModule, TModuleOptions>(
            Action<IConfiguration, IHostEnvironment, TModuleOptions>? configureOptions = null,
            string? optionsKey = null)
            where TModule : IApplicationModule<TModuleOptions>, new() where TModuleOptions : BaseModuleOptions, new()
        {
            if (_moduleRegistrations.ContainsKey(typeof(TModule)))
            {
                throw new Exception($"Module {typeof(TModule)} already registered");
            }

            _moduleRegistrations.Add(typeof(TModule),
                new ApplicationModuleRegistration<TModule, TModuleOptions>(configureOptions, optionsKey));
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
                logger.LogInformation("Init module {Module}", module.Key);
                await module.Value.InitAsync(
                    GetContext(scope.ServiceProvider), scope.ServiceProvider);
            }

            return host;
        }

        protected ApplicationContext GetContext(IServiceProvider serviceProvider)
        {
            return GetContext(serviceProvider.GetRequiredService<IHostEnvironment>(),
                serviceProvider.GetRequiredService<IConfiguration>(),
                serviceProvider.GetRequiredService<ILogger<Application>>());
        }

        protected ApplicationContext GetContext(IHostEnvironment environment, IConfiguration configuration,
            ILogger<Application>? logger = null)
        {
            return new(Name, Version, environment, configuration, logger ?? InternalLogger);
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

        public Application ConfigureLogging(Action<ApplicationContext, LoggerConfiguration, LogLevelSwitcher> configure)
        {
            _loggerConfigurationActions.Add(configure);
            return this;
        }

        public Application ConfigureServices(
            Action<ApplicationContext, HostBuilderContext, IServiceCollection> configure)
        {
            _servicesConfigurationActions.Add(configure);
            return this;
        }

        public Application ConfigureAppConfiguration(
            Action<ApplicationContext, HostBuilderContext, IConfigurationBuilder> configure)
        {
            _appConfigurationActions.Add(configure);
            return this;
        }

        public Application AddModule<TModule>() where TModule : BaseApplicationModule, new()

        {
            RegisterModule<TModule, BaseApplicationModuleOptions>();
            return this;
        }

        public Application AddModule<TModule, TModuleOptions>(
            Action<IConfiguration, IHostEnvironment, TModuleOptions>? configureOptions = null,
            string? optionsKey = null)
            where TModule : IApplicationModule<TModuleOptions>, new()
            where TModuleOptions : BaseModuleOptions, new()
        {
            RegisterModule<TModule, TModuleOptions>(configureOptions, optionsKey);
            return this;
        }
    }

    public static class ApplicationExtensions
    {
        public static TApplication ConfigureServices<TApplication>(this TApplication application,
            Action<ApplicationContext, HostBuilderContext, IServiceCollection> configure)
            where TApplication : Application
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

        public static TApplication AddModule<TApplication, TModule, TModuleOptions>(this TApplication application,
            Action<IConfiguration, IHostEnvironment, TModuleOptions>? configureOptions = null)
            where TApplication : Application
            where TModule : IApplicationModule<TModuleOptions>, new()
            where TModuleOptions : BaseModuleOptions, new()
        {
            application.AddModule<TModule, TModuleOptions>(configureOptions);
            return application;
        }

        public static TApplication AddModule<TApplication, TModule>(this TApplication application)
            where TModule : BaseApplicationModule, new()
            where TApplication : Application
        {
            application.AddModule<TModule, BaseApplicationModuleOptions>();
            return application;
        }

        public static TApplication ConfigureAppConfiguration<TApplication>(this TApplication application,
            Action<ApplicationContext, HostBuilderContext, IConfigurationBuilder> action)
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
        public ILogger Logger { get; }

        public ApplicationContext(string name, string version, IHostEnvironment environment,
            IConfiguration configuration, ILogger logger)
        {
            Name = name;
            Version = version;
            Environment = environment;
            Configuration = configuration;
            Logger = logger;
        }
    }
}
