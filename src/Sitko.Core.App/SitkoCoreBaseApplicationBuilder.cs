using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Sitko.Core.App.Localization;
using Sitko.Core.App.Logging;
using Sitko.Core.App.OpenTelemetry;
using Sitko.FluentValidation;
using Tempus;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Sitko.Core.App;

public abstract class SitkoCoreBaseApplicationBuilder : ISitkoCoreApplicationBuilder
{
    private readonly List<Action> moduleConfigurationCallbacks = new();
    private readonly List<ApplicationModuleRegistration> moduleRegistrations = new();

    private readonly List<Action<IApplicationContext, OpenTelemetryModuleOptions, OpenTelemetryBuilder>>
        openTelemetryConfigureActions = new();

    private readonly SerilogConfigurator serilogConfigurator = new();

    private IApplicationContext? bootApplicationContext;

    private ILogger<ISitkoCoreApplicationBuilder>? internalLogger;

    protected SitkoCoreBaseApplicationBuilder(string[] args, IServiceCollection services,
        IConfigurationBuilder configuration, IApplicationEnvironment environment, ILoggingBuilder logging)
    {
        Args = args.Length != 0 ? args : System.Environment.GetCommandLineArgs().Skip(1).ToArray();
        Services = services;
        Configuration = configuration;
        Environment = environment;
        Logging = logging;
        Init();
    }

    protected ILogger<ISitkoCoreApplicationBuilder> InternalLogger => internalLogger ??
                                                                      throw new InvalidOperationException(
                                                                          "Application init is not executed");

    public string[] Args { get; }
    protected IServiceCollection Services { get; }
    protected IConfigurationBuilder Configuration { get; }
    protected IApplicationEnvironment Environment { get; }
    protected ILoggingBuilder Logging { get; }

    public IApplicationContext Context => bootApplicationContext ?? throw new InvalidOperationException(
        "Application init is not executed");

    public ISitkoCoreApplicationBuilder ConfigureLogLevel(string source, LogEventLevel level)
    {
        serilogConfigurator.ConfigureLogLevel(source, level);
        return this;
    }

    public ISitkoCoreApplicationBuilder ConfigureLogging(
        Func<IApplicationContext, LoggerConfiguration, LoggerConfiguration> configure)
    {
        serilogConfigurator.ConfigureLogging(configure);
        return this;
    }

    public ISitkoCoreApplicationBuilder ConfigureServices(Action<IApplicationContext, IServiceCollection> configure)
    {
        configure(bootApplicationContext!, Services);
        return this;
    }

    public ISitkoCoreApplicationBuilder ConfigureOpenTelemetry(
        Action<IApplicationContext, OpenTelemetryModuleOptions, OpenTelemetryBuilder> configure)
    {
        openTelemetryConfigureActions.Add(configure);
        return this;
    }

    public ISitkoCoreApplicationBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(Services);
        return this;
    }

    public ISitkoCoreApplicationBuilder AddModule<TModule>() where TModule : BaseApplicationModule, new()
    {
        RegisterModule<TModule, BaseApplicationModuleOptions>();
        return this;
    }

    public ISitkoCoreApplicationBuilder AddModule<TModule, TModuleOptions>(
        Action<IApplicationContext, TModuleOptions> configureOptions,
        string? optionsKey = null)
        where TModule : IApplicationModule<TModuleOptions>, new()
        where TModuleOptions : BaseModuleOptions, new()
    {
        RegisterModule<TModule, TModuleOptions>(configureOptions, optionsKey);
        return this;
    }

    public ISitkoCoreApplicationBuilder AddModule<TModule, TModuleOptions>(
        Action<TModuleOptions>? configureOptions = null,
        string? optionsKey = null)
        where TModule : IApplicationModule<TModuleOptions>, new()
        where TModuleOptions : BaseModuleOptions, new() =>
        AddModule<TModule, TModuleOptions>((_, moduleOptions) =>
        {
            configureOptions?.Invoke(moduleOptions);
        }, optionsKey);

    public bool HasModule<TModule>() where TModule : IApplicationModule =>
        moduleRegistrations.Any(r => r.Type == typeof(TModule));

    private void Init()
    {
        var argsProvider = new ApplicationArgsProvider(Args);
        SetupConfiguration(Configuration);
        var bootConfig = Configuration.Build();
        bootApplicationContext = new BuilderApplicationContext(bootConfig, Environment, argsProvider);

        // configure logging
        Configuration.Add(new SerilogDynamicConfigurationSource());
        internalLogger = CreateInternalLogger();
        internalLogger.LogInformation("Start application in {Environment}", Environment.EnvironmentName);
        Logging.ClearProviders();
        Logging.AddSerilog().Configure(options =>
            options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId);
        serilogConfigurator.Configure(ConfigureDefaultLogger);

        AddModule<CommandsModule>();

        Services.AddKeyedSingleton<ILogger>("BootLogger", InternalLogger);
        Services.AddSingleton(typeof(IBootLogger<>), typeof(BootLogger<>));
        Services.AddSingleton<IApplicationArgsProvider>(argsProvider);
        Services.AddSingleton(Environment);
        Services.AddSingleton(bootApplicationContext);
        Services.AddTransient<IScheduler, Scheduler>();
        Services.AddFluentValidationExtensions();
        Services.AddTransient(typeof(ILocalizationProvider<>), typeof(LocalizationProvider<>));
        Services.AddSingleton<IApplicationLifecycle, ApplicationLifecycle>(); // только Hosted? Проверить для Wasm
        Services.AddHostedService<HostedLifecycleService>(); // только Hosted? Проверить для Wasm
    }

    private ILogger<ISitkoCoreApplicationBuilder> CreateInternalLogger()
    {
        var tmpLoggerConfiguration = new LoggerConfiguration();
        tmpLoggerConfiguration = ConfigureDefaultLogger(tmpLoggerConfiguration);
        if (Context.Options.EnableConsoleLogging != true)
        {
            tmpLoggerConfiguration = tmpLoggerConfiguration.WriteTo.Console(
                outputTemplate: ApplicationOptions.BaseConsoleLogFormat,
                formatProvider: CultureInfo.InvariantCulture,
                restrictedToMinimumLevel: LogEventLevel.Debug);
        }

        tmpLoggerConfiguration.MinimumLevel.Warning();
        tmpLoggerConfiguration.MinimumLevel.Override("Sitko.Core", LogEventLevel.Information);
        var tmpLogger = tmpLoggerConfiguration.CreateLogger();
        Log.Logger = tmpLogger; // set default logger until host is started
        Console.OutputEncoding = Encoding.UTF8;
        return new SerilogLoggerFactory(tmpLogger)
            .CreateLogger<ISitkoCoreApplicationBuilder>();
    }

    private void RegisterModule<TModule, TModuleOptions>(
        Action<IApplicationContext, TModuleOptions>? configureOptions = null,
        string? optionsKey = null)
        where TModule : IApplicationModule<TModuleOptions>, new() where TModuleOptions : BaseModuleOptions, new()
    {
        var instance = new TModule();
        if (!instance.AllowMultiple && HasModule<TModule>())
        {
            throw new InvalidOperationException($"Module {typeof(TModule)} already registered");
        }

        var registration =
            new ApplicationModuleRegistration<TModule, TModuleOptions>(instance, configureOptions, optionsKey);
        if (registration.IsEnabled(Context))
        {
            registration.ConfigureAppConfiguration(Context, Configuration);
        }

        moduleRegistrations.Add(registration);
        moduleConfigurationCallbacks.Add(() =>
        {
            if (registration.IsEnabled(Context))
            {
                BeforeModuleRegistration<TModule, TModuleOptions>(Context, registration);

                registration.ConfigureOptions(Context, Services);
                registration.ConfigureServices(Context, Services);

                AfterModuleRegistration<TModule, TModuleOptions>(Context, registration);
            }
        });
        Services.AddSingleton<ApplicationModuleRegistration>(registration);
    }

    protected virtual void BeforeModuleRegistration<TModule, TModuleOptions>(IApplicationContext applicationContext,
        ApplicationModuleRegistration moduleRegistration) where TModule : IApplicationModule<TModuleOptions>, new()
        where TModuleOptions : BaseModuleOptions, new()
    {
    }

    protected virtual void AfterModuleRegistration<TModule, TModuleOptions>(IApplicationContext applicationContext,
        ApplicationModuleRegistration moduleRegistration) where TModule : IApplicationModule<TModuleOptions>, new()
        where TModuleOptions : BaseModuleOptions, new()
    {
    }

    protected virtual IConfigurationBuilder SetupConfiguration(IConfigurationBuilder configurationBuilder) =>
        configurationBuilder;

    protected virtual LoggerConfiguration ConfigureDefaultLogger(LoggerConfiguration loggerConfiguration)
    {
        loggerConfiguration = loggerConfiguration.ReadFrom.Configuration(Context.Configuration);
        loggerConfiguration = loggerConfiguration
            .Enrich.FromLogContext()
            .Enrich.WithProperty("App", Context.Name)
            .Enrich.WithProperty("AppVersion", Context.Version)
            .Enrich.WithProperty("AppEnvironment", Context.Environment)
            .Enrich.WithMachineName()
            // TODO: Убрать после обновления на Serilog.Extensions.Logging 8.0.1+
            .Enrich.With<TraceMetaEnricher>();

        if (Context.Options.EnableConsoleLogging == true)
        {
            loggerConfiguration = loggerConfiguration.WriteTo.Console(
                outputTemplate: Context.Options.ConsoleLogFormat,
                formatProvider: CultureInfo.InvariantCulture);
        }

        return loggerConfiguration;
    }

    protected virtual void BeforeContainerBuild()
    {
        var enabledModulesBeforeOpenTelemetry =
            ModulesHelper.GetEnabledModuleRegistrations(Context, moduleRegistrations);

        AddModule<OpenTelemetryModule, OpenTelemetryModuleOptions>((context, options) =>
        {
            foreach (var openTelemetryConfigureAction in openTelemetryConfigureActions)
            {
                options.Configure(openTelemetryConfigureAction);
            }

            foreach (var moduleRegistration in ModulesHelper.GetEnabledModuleRegistrations<IOpenTelemetryModule>(
                         context, enabledModulesBeforeOpenTelemetry))
            {
                options.Configure((applicationContext, _, opentTelemetryBuilder) =>
                {
                    moduleRegistration.ConfigureOpenTelemetry(applicationContext, opentTelemetryBuilder);
                });
            }
        });

        var enabledModules = ModulesHelper.GetEnabledModuleRegistrations(Context, moduleRegistrations);
        foreach (var applicationModuleRegistration in enabledModules)
        {
            applicationModuleRegistration.ClearOptionsCache();
        }

        serilogConfigurator.ApplyLogging(Context, enabledModules);
        foreach (var postConfigureCallback in moduleConfigurationCallbacks)
        {
            postConfigureCallback();
        }

        foreach (var moduleRegistration in enabledModules)
        {
            moduleRegistration.PostConfigureServices(Context, Services);
        }
    }

    protected void LogInternal(string message) => InternalLogger.LogInformation("Check log: {Message}", message);
}
