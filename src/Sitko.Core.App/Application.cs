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
using Sitko.Core.App.Validation;
using Tempus;
using Thinktecture;
using Thinktecture.Extensions.Configuration;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Sitko.Core.App
{
    using System.Text.Json;
    using JetBrains.Annotations;
    using Microsoft.Extensions.FileProviders;

    public abstract class Application : IApplication, IAsyncDisposable
    {
        private readonly List<Action<ApplicationContext, HostBuilderContext, IConfigurationBuilder>>
            appConfigurationActions = new();

        private readonly string[] args;
        private readonly List<ApplicationCommand> commands = new();

        private ApplicationCommand? currentCommand;

        private readonly Dictionary<string, LogEventLevel> logEventLevels = new();

        private readonly List<Action<ApplicationContext, LoggerConfiguration>>
            loggerConfigurationActions = new();

        private readonly Dictionary<Type, ApplicationModuleRegistration> moduleRegistrations =
            new();

        private readonly List<Action<ApplicationContext, HostBuilderContext, IServiceCollection>>
            servicesConfigurationActions = new();

        private readonly Dictionary<string, object> store = new();

        private IHost? appHost;

        protected Application(string[] args)
        {
            this.args = args;
            Console.OutputEncoding = Encoding.UTF8;
            var loggerConfiguration = new LoggerConfiguration();
            loggerConfiguration
                .WriteTo.Console(outputTemplate: ApplicationOptions.BaseConsoleLogFormat,
                    restrictedToMinimumLevel: LogEventLevel.Debug);
            InternalLogger = new SerilogLoggerFactory(loggerConfiguration.CreateLogger()).CreateLogger<Application>();
            ProcessArguments();
        }

        private void ProcessArguments()
        {
            commands.Add(new("check", true, true, OnAfterRunAsync: () =>
            {
                LogVerbose("Check run is successful. Exit");
                return Task.FromResult(false);
            }));
            commands.Add(new("generate-options", true, OnBeforeRunAsync:
                () =>
                {
                    InternalLogger.LogInformation("Generate options");

                    var modulesOptions = GetModulesOptions(GetContext(new DummyEnvironment(),
                        new ConfigurationRoot(new List<IConfigurationProvider>())));

                    InternalLogger.LogInformation("Modules options:");
                    InternalLogger.LogInformation("{Options}", JsonSerializer.Serialize(modulesOptions,
                        new JsonSerializerOptions { WriteIndented = true }));
                    return Task.FromResult(false);
                }));
            commands.Add(new("run"));
            if (args.Length > 0)
            {
                var commandName = args[0];
                currentCommand = GetCommand(commandName);
                if (currentCommand is null)
                {
                    throw new ArgumentException($"Unknown command {commandName}. Supported commands: {commands}",
                        nameof(args));
                }
                else
                {
                    InternalLogger.LogInformation("Run command {CommandName}", currentCommand.Name);
                }
            }
        }

        protected virtual string ApplicationOptionsKey => nameof(Application);

        public Guid Id { get; } = Guid.NewGuid();

        private ILogger<Application> InternalLogger { get; set; }

        public string Name => GetApplicationOptions().Name;
        public string Version => GetApplicationOptions().Version;

        public virtual ValueTask DisposeAsync()
        {
            appHost?.Dispose();
            GC.SuppressFinalize(this);
            return new ValueTask();
        }

        private ApplicationCommand? GetCommand(string commandName) =>
            commands.FirstOrDefault(c => c.Name == commandName);


        [PublicAPI]
        public ApplicationOptions GetApplicationOptions() =>
            GetApplicationOptions(GetContext().Environment, GetContext().Configuration);

        [PublicAPI]
        protected ApplicationOptions GetApplicationOptions(IHostEnvironment environment, IConfiguration configuration)
        {
            var options = new ApplicationOptions();
            configuration.Bind(ApplicationOptionsKey, options);
            if (string.IsNullOrEmpty(options.Name))
            {
                options.Name = GetType().Assembly.GetName().Name ?? "App";
            }

            if (string.IsNullOrEmpty(options.Version))
            {
                options.Version = GetType().Assembly.GetName().Version?.ToString() ?? "dev";
            }

            options.EnableConsoleLogging ??= environment.IsDevelopment();

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

        private void LogVerbose(string message)
        {
            if (currentCommand?.EnableVerboseLogging == true)
            {
                InternalLogger.LogInformation("Check log: {Message}", message);
            }
        }

        private IHostBuilder ConfigureHostBuilder(Action<IHostBuilder>? configure = null)
        {
            LogVerbose("Configure host builder start");

            LogVerbose("Create tmp host builder");

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

            LogVerbose("Init application");

            InitApplication();

            LogVerbose("Create main host builder");
            var loggingConfiguration = new SerilogConfiguration();
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
                    LogVerbose("Configure app configuration");
                    builder.AddLoggingConfiguration(loggingConfiguration, "Serilog");
                    var appContext = GetContext(context.HostingEnvironment, context.Configuration);
                    foreach (var appConfigurationAction in appConfigurationActions)
                    {
                        appConfigurationAction(appContext, context, builder);
                    }

                    LogVerbose("Configure app configuration in modules");
                    foreach (var moduleRegistration in GetEnabledModuleRegistrations(tmpApplicationContext))
                    {
                        moduleRegistration.ConfigureAppConfiguration(appContext, builder);
                    }
                })
                .ConfigureServices((context, services) =>
                {
                    LogVerbose("Configure app services");
                    services.AddSingleton<ISerilogConfiguration>(loggingConfiguration);
                    services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory());
                    services.AddSingleton(typeof(IApplication), this);
                    services.AddSingleton(typeof(Application), this);
                    services.AddSingleton(GetType(), this);
                    services.AddHostedService<ApplicationLifetimeService>();
                    services.AddTransient<IScheduler, Scheduler>();
                    services.AddScoped<FluentGraphValidator>();
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
                }).ConfigureLogging((context, builder) =>
                {
                    var applicationOptions = GetApplicationOptions(context.HostingEnvironment, context.Configuration);
                    LogVerbose("Configure logging");
                    builder.AddConfiguration(context.Configuration.GetSection("Logging"));
                    var loggerConfiguration = new LoggerConfiguration();
                    loggerConfiguration.ReadFrom.Configuration(context.Configuration);
                    loggerConfiguration
                        .Enrich.FromLogContext()
                        .Enrich.WithMachineName()
                        .Enrich.WithProperty("App", applicationOptions.Name)
                        .Enrich.WithProperty("AppVersion", applicationOptions.Version);

                    if (applicationOptions.EnableConsoleLogging == true)
                    {
                        loggerConfiguration.WriteTo.Console(outputTemplate: applicationOptions.ConsoleLogFormat);
                    }

                    var appContext = GetContext(context.HostingEnvironment, context.Configuration);
                    ConfigureLogging(appContext,
                        loggerConfiguration);
                    foreach (var (key, value) in
                        logEventLevels)
                    {
                        loggerConfiguration.MinimumLevel.Override(key, value);
                    }

                    foreach (var moduleRegistration in GetEnabledModuleRegistrations(appContext))
                    {
                        moduleRegistration.ConfigureLogging(tmpApplicationContext, loggerConfiguration);
                    }

                    foreach (var loggerConfigurationAction in loggerConfigurationActions)
                    {
                        loggerConfigurationAction(appContext, loggerConfiguration);
                    }

                    Log.Logger = loggerConfiguration.CreateLogger();
                });

            LogVerbose("Configure host builder in modules");
            foreach (var moduleRegistration in GetEnabledModuleRegistrations(tmpApplicationContext))
            {
                moduleRegistration.ConfigureHostBuilder(tmpApplicationContext, hostBuilder);
            }

            LogVerbose("Configure host builder");
            configure?.Invoke(hostBuilder);
            LogVerbose("Create host builder done");
            return hostBuilder;
        }

        protected IHost CreateAppHost(Action<IHostBuilder>? configure = null)
        {
            LogVerbose("Create app host start");

            if (appHost is not null)
            {
                LogVerbose("App host is already built");

                return appHost;
            }

            LogVerbose("Configure host builder");

            var hostBuilder = ConfigureHostBuilder(configure);

            LogVerbose("Build host");
            var host = hostBuilder.Build();

            appHost = host;
            LogVerbose("Create app host done");
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

        protected virtual void ConfigureLogging(ApplicationContext applicationContext,
            LoggerConfiguration loggerConfiguration)
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
            if (currentCommand?.OnBeforeRunAsync is not null)
            {
                var shouldContinue = await currentCommand.OnBeforeRunAsync();
                if (!shouldContinue)
                {
                    return;
                }
            }

            LogVerbose("Run app start");
            LogVerbose("Build and init");
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


            if (currentCommand?.OnAfterRunAsync is not null)
            {
                var shouldContinue = await currentCommand.OnAfterRunAsync();
                if (!shouldContinue)
                {
                    return;
                }
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
                var logger = serviceProvider.GetRequiredService<ILogger<Application>>();
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
                throw new InvalidOperationException($"Module {typeof(TModule)} already registered");
            }

            moduleRegistrations.Add(typeof(TModule),
                new ApplicationModuleRegistration<TModule, TModuleOptions>(configureOptions, optionsKey));
        }

        protected virtual void InitApplication()
        {
        }

        public async Task<IHost> BuildAndInitAsync(Action<IHostBuilder>? configure = null)
        {
            LogVerbose("Build and init async start");

            var host = CreateAppHost(configure);

            if (currentCommand?.IsInitDisabled != true)
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

            LogVerbose("Build and init async done");

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
            return new ApplicationContext(applicationOptions.Name, applicationOptions.Version, environment,
                configuration,
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

        public Application ConfigureLogging(Action<ApplicationContext, LoggerConfiguration> configure)
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

    internal class DummyEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "";
        public string ApplicationName { get; set; } = "";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    internal record ApplicationCommand(string Name, bool IsInitDisabled = false, bool EnableVerboseLogging = false,
        Func<Task<bool>>? OnBeforeRunAsync = null,
        Func<Task<bool>>? OnAfterRunAsync = null);
}
