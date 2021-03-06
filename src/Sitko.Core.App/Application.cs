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
using Sitko.Core.App.Localization;
using Sitko.Core.App.Logging;
using Tempus;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Sitko.Core.App
{
    using System.Text.Json;
    using JetBrains.Annotations;
    using Microsoft.Extensions.Hosting.Internal;

    public abstract class Application : IApplication, IAsyncDisposable
    {
        private const string CheckCommand = "check";
        private const string GenerateOptionsCommand = "generate-options";

        private readonly List<Action<ApplicationContext, HostBuilderContext, IConfigurationBuilder>>
            appConfigurationActions = new();

        private readonly string[] args;

        private readonly string? currentCommand;

        private readonly Dictionary<string, LogEventLevel> logEventLevels = new();

        private readonly List<Action<ApplicationContext, LoggerConfiguration, LogLevelSwitcher>>
            loggerConfigurationActions = new();

        private readonly LogLevelSwitcher logLevelSwitcher = new();

        private readonly Dictionary<Type, ApplicationModuleRegistration> moduleRegistrations =
            new();

        private readonly List<Action<ApplicationContext, HostBuilderContext, IServiceCollection>>
            servicesConfigurationActions = new();

        private readonly Dictionary<string, object> store = new();
        private readonly string[] supportedCommands = {CheckCommand, GenerateOptionsCommand};

        private IHost? appHost;

        protected Application(string[] args)
        {
            this.args = args;
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length > 0)
            {
                var command = args[0];
                if (supportedCommands.Contains(command))
                {
                    currentCommand = command;
                }
                else
                {
                    throw new ArgumentException($"Unknown command {command}. Supported commands: {supportedCommands}",
                        nameof(args));
                }
            }

            var loggerConfiguration = new LoggerConfiguration();
            loggerConfiguration
                .WriteTo.Console(outputTemplate: ApplicationOptions.BaseConsoleLogFormat,
                    restrictedToMinimumLevel: LogEventLevel.Debug);
            InternalLogger = new SerilogLoggerFactory(loggerConfiguration.CreateLogger()).CreateLogger<Application>();
        }

        protected virtual string ApplicationOptionsKey => nameof(Application);

        public Guid Id { get; } = Guid.NewGuid();

        private ILogger<Application> InternalLogger { get; set; }

        private bool IsCheckRun => currentCommand == CheckCommand;

        public string Name => GetApplicationOptions().Name;
        public string Version => GetApplicationOptions().Version;

        public virtual ValueTask DisposeAsync()
        {
            appHost?.Dispose();
            return new ValueTask();
        }


        [PublicAPI]
        public ApplicationOptions GetApplicationOptions() =>
            GetApplicationOptions(GetContext().Environment, GetContext().Configuration);

        [PublicAPI]
        protected ApplicationOptions GetApplicationOptions(IHostEnvironment environment, IConfiguration configuration)
        {
            var options = new ApplicationOptions();
            configuration.Bind(ApplicationOptionsKey, options);
            ConfigureApplicationOptions(environment, configuration, options);
            return options;
        }

        protected virtual void ConfigureApplicationOptions(IHostEnvironment environment, IConfiguration configuration,
            ApplicationOptions options)
        {
        }

        protected IReadOnlyList<ApplicationModuleRegistration>
            GetEnabledModuleRegistrations(ApplicationContext context) => moduleRegistrations
            .Where(r => r.Value.IsEnabled(context)).Select(r => r.Value).ToList();

        private void LogCheck(string message)
        {
            if (IsCheckRun)
            {
                InternalLogger.LogInformation("Check log: {Message}", message);
            }
        }

        private IHostBuilder ConfigureHostBuilder(Action<IHostBuilder>? configure = null)
        {
            LogCheck("Configure host builder start");

            LogCheck("Create tmp host builder");

            using var tmpHost = CreateHostBuilder(args)
                .UseDefaultServiceProvider(options =>
                {
                    options.ValidateOnBuild = false;
                    options.ValidateScopes = true;
                })
                .ConfigureLogging(builder => { builder.SetMinimumLevel(LogLevel.Information); }).Build();

            var tmpConfiguration = tmpHost.Services.GetRequiredService<IConfiguration>();
            var tmpEnvironment = tmpHost.Services.GetRequiredService<IHostEnvironment>();

            var tmpApplicationContext = GetContext(tmpEnvironment, tmpConfiguration);

            LogCheck("Init application");

            InitApplication();

            LogCheck("Create main host builder");

            var hostBuilder = CreateHostBuilder(args)
                .UseDefaultServiceProvider(options =>
                {
                    options.ValidateOnBuild = true;
                    options.ValidateScopes = true;
                })
                .ConfigureHostConfiguration(builder =>
                {
                    builder.AddJsonFile("appsettings.json", true, false);
                    builder.AddJsonFile($"appsettings.{tmpApplicationContext.Environment.EnvironmentName}.json", true,
                        false);
                })
                .ConfigureAppConfiguration((context, builder) =>
                {
                    LogCheck("Configure app configuration");
                    var appContext = GetContext(context.HostingEnvironment, context.Configuration);
                    foreach (var appConfigurationAction in appConfigurationActions)
                    {
                        appConfigurationAction(appContext, context, builder);
                    }

                    LogCheck("Configure app configuration in modules");
                    foreach (var moduleRegistration in GetEnabledModuleRegistrations(tmpApplicationContext))
                    {
                        moduleRegistration.ConfigureAppConfiguration(appContext, context, builder);
                    }
                })
                .ConfigureServices((context, services) =>
                {
                    LogCheck("Configure app services");
                    services.AddSingleton(logLevelSwitcher);
                    services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory());
                    services.AddSingleton(typeof(IApplication), this);
                    services.AddSingleton(typeof(Application), this);
                    services.AddSingleton(GetType(), this);
                    services.AddHostedService<ApplicationLifetimeService>();
                    services.AddTransient<IScheduler, Scheduler>();
                    services.AddTransient(typeof(ILocalizationProvider<>), typeof(LocalizationProvider<>));

                    var appContext = GetContext(context.HostingEnvironment, context.Configuration);
                    foreach (var servicesConfigurationAction in servicesConfigurationActions)
                    {
                        servicesConfigurationAction(appContext, context, services);
                    }

                    foreach (var moduleRegistration in GetEnabledModuleRegistrations(appContext))
                    {
                        moduleRegistration.ConfigureOptions(appContext, services);
                        moduleRegistration.ConfigureServices(appContext, services);
                    }
                }).ConfigureLogging((context, _) =>
                {
                    var applicationOptions = GetApplicationOptions(context.HostingEnvironment, context.Configuration);
                    LogCheck("Configure logging");
                    var loggerConfiguration = new LoggerConfiguration();
                    loggerConfiguration.MinimumLevel.ControlledBy(logLevelSwitcher.Switch);
                    loggerConfiguration
                        .Enrich.FromLogContext()
                        .Enrich.WithMachineName()
                        .Enrich.WithProperty("App", applicationOptions.Name)
                        .Enrich.WithProperty("AppVersion", applicationOptions.Version);
                    logLevelSwitcher.Switch.MinimumLevel =
                        context.HostingEnvironment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information;

                    if (LoggingEnableConsole(context))
                    {
                        loggerConfiguration
                            .WriteTo.Console(
                                outputTemplate: applicationOptions.ConsoleLogFormat,
                                levelSwitch: logLevelSwitcher.Switch);
                    }

                    var appContext = GetContext(context.HostingEnvironment, context.Configuration);
                    ConfigureLogging(appContext,
                        loggerConfiguration,
                        logLevelSwitcher);
                    foreach (var (key, value) in
                        logEventLevels)
                    {
                        loggerConfiguration.MinimumLevel.Override(key, value);
                    }

                    foreach (var moduleRegistration in GetEnabledModuleRegistrations(appContext))
                    {
                        moduleRegistration.ConfigureLogging(tmpApplicationContext, loggerConfiguration,
                            logLevelSwitcher);
                    }

                    foreach (var loggerConfigurationAction in loggerConfigurationActions)
                    {
                        loggerConfigurationAction(appContext, loggerConfiguration, logLevelSwitcher);
                    }

                    Log.Logger = loggerConfiguration.CreateLogger();
                });

            LogCheck("Configure host builder in modules");
            foreach (var moduleRegistration in GetEnabledModuleRegistrations(tmpApplicationContext))
            {
                moduleRegistration.ConfigureHostBuilder(tmpApplicationContext, hostBuilder);
            }

            LogCheck("Configure host builder");
            configure?.Invoke(hostBuilder);
            LogCheck("Create host builder done");
            return hostBuilder;
        }

        protected IHost CreateAppHost(Action<IHostBuilder>? configure = null)
        {
            LogCheck("Create app host start");

            if (appHost is not null)
            {
                LogCheck("App host is already built");

                return appHost;
            }

            LogCheck("Configure host builder");

            var hostBuilder = ConfigureHostBuilder(configure);

            LogCheck("Build host");
            var host = hostBuilder.Build();

            appHost = host;
            LogCheck("Create app host done");
            return appHost;
        }

        private IHostBuilder CreateHostBuilder(string[] hostBuilderArgs)
        {
            var builder = Host.CreateDefaultBuilder(hostBuilderArgs);
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
            LogLevelSwitcher appLogLevelSwitcher)
        {
        }

        [PublicAPI]
        public Dictionary<string, object> GetModulesOptions() => GetModulesOptions(GetContext());


        private Dictionary<string, object> GetModulesOptions(ApplicationContext applicationContext)
        {
            var modulesOptions = new Dictionary<string, object>
            {
                {
                    ApplicationOptionsKey,
                    GetApplicationOptions(applicationContext.Environment, applicationContext.Configuration)
                }
            };
            foreach (var moduleRegistration in moduleRegistrations.Values)
            {
                var (configKey, options) = moduleRegistration.GetOptions(applicationContext);
                if (!string.IsNullOrEmpty(configKey))
                {
                    var current = modulesOptions;
                    var parts = configKey.Split(':');
                    for (var i = 0; i < parts.Length; i++)
                    {
                        if (i == parts.Length - 1)
                        {
                            current[parts[i]] = options;
                        }
                        else
                        {
                            if (current.ContainsKey(parts[i]))
                            {
                                current = (Dictionary<string, object>)current[parts[i]];
                            }
                            else
                            {
                                var part = new Dictionary<string, object>();
                                current[parts[i]] = part;
                                current = part;
                            }
                        }
                    }
                }
            }

            return modulesOptions;
        }

        public async Task RunAsync()
        {
            if (currentCommand == GenerateOptionsCommand)
            {
                InternalLogger.LogInformation("Generate options");

                var modulesOptions = GetModulesOptions(GetContext(new HostingEnvironment(),
                    new ConfigurationRoot(new List<IConfigurationProvider>())));

                InternalLogger.LogInformation("Modules options:");
                InternalLogger.LogInformation("{Options}", JsonSerializer.Serialize(modulesOptions,
                    new JsonSerializerOptions {WriteIndented = true}));

                return;
            }

            LogCheck("Run app start");
            LogCheck("Build and init");
            var host = await BuildAndInitAsync();

            InternalLogger.LogInformation("Check required modules");
            var context = GetContext(host.Services);
            var modulesCheckSuccess = true;
            foreach (var registration in GetEnabledModuleRegistrations(context))
            {
                var result =
                    registration.CheckRequiredModules(context,
                        GetEnabledModuleRegistrations(context).Select(r => r.Type).ToArray());
                if (!result.isSuccess)
                {
                    foreach (var missingModule in result.missingModules)
                    {
                        InternalLogger.LogCritical(
                            "Required module {MissingModule} for module {Module} is not registered",
                            missingModule, registration.Type);
                    }

                    modulesCheckSuccess = false;
                }
            }

            if (!modulesCheckSuccess)
            {
                InternalLogger.LogError("Check required modules failed");
                return;
            }


            if (IsCheckRun)
            {
                LogCheck("Check run is successful. Exit");
                return;
            }

            await host.RunAsync();
        }

        public async Task<IHost> StartAsync()
        {
            var host = await BuildAndInitAsync();

            await host.StartAsync();
            return host;
        }

        public async Task StopAsync() => await CreateAppHost().StopAsync();

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

        public IHostBuilder GetHostBuilder() => ConfigureHostBuilder();

        public IServiceProvider GetServiceProvider() => CreateAppHost().Services;

        public T? GetService<T>() => GetServiceProvider().GetService<T>();

        [PublicAPI]
        protected void RegisterModule<TModule, TModuleOptions>(
            Action<IConfiguration, IHostEnvironment, TModuleOptions>? configureOptions = null,
            string? optionsKey = null)
            where TModule : IApplicationModule<TModuleOptions>, new() where TModuleOptions : BaseModuleOptions, new()
        {
            if (moduleRegistrations.ContainsKey(typeof(TModule)))
            {
                throw new Exception($"Module {typeof(TModule)} already registered");
            }

            moduleRegistrations.Add(typeof(TModule),
                new ApplicationModuleRegistration<TModule, TModuleOptions>(configureOptions, optionsKey));
        }

        protected virtual void InitApplication()
        {
        }

        public async Task<IHost> BuildAndInitAsync(Action<IHostBuilder>? configure = null)
        {
            LogCheck("Build and init async start");

            var host = CreateAppHost(configure);

            if (string.IsNullOrEmpty(currentCommand))
            {
                using var scope = host.Services.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Application>>();
                logger.LogInformation("Init modules");
                foreach (var module in GetEnabledModuleRegistrations(GetContext(scope.ServiceProvider)))
                {
                    logger.LogInformation("Init module {Module}", module.Type);
                    await module.InitAsync(
                        GetContext(scope.ServiceProvider), scope.ServiceProvider);
                }
            }

            LogCheck("Build and init async done");

            return host;
        }

        [PublicAPI]
        protected ApplicationContext GetContext() => appHost is not null
            ? GetContext(appHost.Services)
            : throw new InvalidOperationException("App host is not built yet");

        [PublicAPI]
        protected ApplicationContext GetContext(IServiceProvider serviceProvider) =>
            GetContext(serviceProvider.GetRequiredService<IHostEnvironment>(),
                serviceProvider.GetRequiredService<IConfiguration>(),
                serviceProvider.GetRequiredService<ILogger<Application>>());

        protected ApplicationContext GetContext(IHostEnvironment environment, IConfiguration configuration,
            ILogger<Application>? logger = null)
        {
            var applicationOptions = GetApplicationOptions(environment, configuration);
            return new ApplicationContext(applicationOptions.Name, applicationOptions.Version, environment, configuration,
                logger ?? InternalLogger);
        }

        public async Task OnStarted(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Application>>();
            await OnStartedAsync(configuration, environment, serviceProvider);
            foreach (var moduleRegistration in GetEnabledModuleRegistrations(GetContext(serviceProvider)))
            {
                try
                {
                    await moduleRegistration.ApplicationStarted(configuration, environment, serviceProvider);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error on application started hook in module {Module}: {ErrorText}",
                        moduleRegistration.Type,
                        ex.ToString());
                }
            }
        }

        protected virtual Task OnStartedAsync(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider) =>
            Task.CompletedTask;

        public async Task OnStopping(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Application>>();
            await OnStoppingAsync(configuration, environment, serviceProvider);
            foreach (var moduleRegistration in GetEnabledModuleRegistrations(GetContext(serviceProvider)))
            {
                try
                {
                    await moduleRegistration.ApplicationStopping(configuration, environment, serviceProvider);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error on application stopping hook in module {Module}: {ErrorText}",
                        moduleRegistration.Type,
                        ex.ToString());
                }
            }
        }

        protected virtual Task OnStoppingAsync(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider) =>
            Task.CompletedTask;

        public async Task OnStopped(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Application>>();
            await OnStoppedAsync(configuration, environment, serviceProvider);
            foreach (var moduleRegistration in GetEnabledModuleRegistrations(GetContext(serviceProvider)))
            {
                try
                {
                    await moduleRegistration.ApplicationStopped(configuration, environment, serviceProvider);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error on application stopped hook in module {Module}: {ErrorText}",
                        moduleRegistration.Type,
                        ex.ToString());
                }
            }
        }

        protected virtual Task OnStoppedAsync(IConfiguration configuration, IHostEnvironment environment,
            IServiceProvider serviceProvider) =>
            Task.CompletedTask;

        public bool HasModule<TModule>() where TModule : IApplicationModule =>
            moduleRegistrations.ContainsKey(typeof(TModule));


        public void Set(string key, object value) => store[key] = value;

        public T Get<T>(string key)
        {
            if (store.ContainsKey(key))
            {
                return (T)store[key];
            }

#pragma warning disable 8603
            return default;
#pragma warning restore 8603
        }

        public Application ConfigureLogLevel(string source, LogEventLevel level)
        {
            logEventLevels.Add(source, level);
            return this;
        }

        public Application ConfigureLogging(Action<ApplicationContext, LoggerConfiguration, LogLevelSwitcher> configure)
        {
            loggerConfigurationActions.Add(configure);
            return this;
        }

        public Application ConfigureServices(
            Action<ApplicationContext, HostBuilderContext, IServiceCollection> configure)
        {
            servicesConfigurationActions.Add(configure);
            return this;
        }

        public Application ConfigureServices(Action<IServiceCollection> configure)
        {
            servicesConfigurationActions.Add((_, _, services) =>
            {
                configure(services);
            });
            return this;
        }

        public Application ConfigureAppConfiguration(
            Action<ApplicationContext, HostBuilderContext, IConfigurationBuilder> configure)
        {
            appConfigurationActions.Add(configure);
            return this;
        }

        public Application AddModule<TModule>() where TModule : BaseApplicationModule, new()

        {
            RegisterModule<TModule, BaseApplicationModuleOptions>();
            return this;
        }

        public Application AddModule<TModule, TModuleOptions>(
            Action<IConfiguration, IHostEnvironment, TModuleOptions> configureOptions,
            string? optionsKey = null)
            where TModule : IApplicationModule<TModuleOptions>, new()
            where TModuleOptions : BaseModuleOptions, new()
        {
            RegisterModule<TModule, TModuleOptions>(configureOptions, optionsKey);
            return this;
        }

        public Application AddModule<TModule, TModuleOptions>(
            Action<TModuleOptions>? configureOptions = null,
            string? optionsKey = null)
            where TModule : IApplicationModule<TModuleOptions>, new()
            where TModuleOptions : BaseModuleOptions, new() =>
            AddModule<TModule, TModuleOptions>((_, _, moduleOptions) =>
            {
                configureOptions?.Invoke(moduleOptions);
            }, optionsKey);
    }

    [PublicAPI]
    public class ApplicationContext
    {
        public ApplicationContext(string name, string version, IHostEnvironment environment,
            IConfiguration configuration, ILogger logger)
        {
            Name = name;
            Version = version;
            Environment = environment;
            Configuration = configuration;
            Logger = logger;
        }

        public string Name { get; }
        public string Version { get; }
        public IHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }
        public ILogger Logger { get; }
    }
}
