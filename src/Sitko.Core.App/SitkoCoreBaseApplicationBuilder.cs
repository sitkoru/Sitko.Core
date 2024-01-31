using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Sitko.Core.App.Localization;
using Sitko.Core.App.Logging;
using Sitko.FluentValidation;
using Tempus;

namespace Sitko.Core.App;

public abstract class SitkoCoreBaseApplicationBuilder : ISitkoCoreApplicationBuilder
{
    private readonly List<ApplicationModuleRegistration> moduleRegistrations = new();
    private readonly SerilogConfigurator serilogConfigurator = new();

    private IApplicationContext? bootApplicationContext;

    private ILogger<ISitkoCoreApplicationBuilder>? internalLogger;

    protected SitkoCoreBaseApplicationBuilder(string[] args, IServiceCollection services,
        IConfigurationBuilder configuration, IApplicationEnvironment environment, ILoggingBuilder logging)
    {
        Args = args;
        Services = services;
        Configuration = configuration;
        Environment = environment;
        Logging = logging;
        Init();
    }

    protected ILogger<ISitkoCoreApplicationBuilder> InternalLogger => internalLogger ??
                                                                      throw new InvalidOperationException(
                                                                          "Application init is not executed");

    protected IApplicationContext BootApplicationContext => bootApplicationContext ??
                                                            throw new InvalidOperationException(
                                                                "Application init is not executed");

    public string[] Args { get; }
    protected IServiceCollection Services { get; }
    protected IConfigurationBuilder Configuration { get; }
    protected IApplicationEnvironment Environment { get; }
    protected ILoggingBuilder Logging { get; }

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
        Logging.ClearProviders();
        Logging.AddSerilog();
        serilogConfigurator.Configure(ConfigureDefautLogger);

        AddModule<CommandsModule>();

        Services.AddSingleton<IApplicationArgsProvider>(argsProvider);
        Services.AddSingleton(Environment);
        Services.AddSingleton<IApplicationContext, BuilderApplicationContext>();
        Services.AddTransient<IScheduler, Scheduler>();
        Services.AddFluentValidationExtensions();
        Services.AddTransient(typeof(ILocalizationProvider<>), typeof(LocalizationProvider<>));
        Services.AddSingleton<IApplicationLifecycle, ApplicationLifecycle>(); // только Hosted? Проверить для Wasm
        Services.AddHostedService<HostedLifecycleService>(); // только Hosted? Проверить для Wasm
    }

    private ILogger<ISitkoCoreApplicationBuilder> CreateInternalLogger()
    {
        var tmpLoggerConfiguration = new LoggerConfiguration();
        tmpLoggerConfiguration = ConfigureDefautLogger(tmpLoggerConfiguration);
        if (BootApplicationContext.Options.EnableConsoleLogging != true)
        {
            tmpLoggerConfiguration = tmpLoggerConfiguration.WriteTo.Console(
                outputTemplate: ApplicationOptions.BaseConsoleLogFormat,
                formatProvider: CultureInfo.InvariantCulture,
                restrictedToMinimumLevel: LogEventLevel.Debug);
        }

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
        if (registration.IsEnabled(BootApplicationContext))
        {
            BeforeModuleRegistration<TModule, TModuleOptions>(BootApplicationContext, registration);

            registration.ConfigureAppConfiguration(BootApplicationContext, Configuration);
            registration.ConfigureOptions(BootApplicationContext, Services);
            registration.ConfigureServices(BootApplicationContext, Services);

            AfterModuleRegistration<TModule, TModuleOptions>(BootApplicationContext, registration);
        }

        moduleRegistrations.Add(registration);
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

    protected virtual LoggerConfiguration ConfigureDefautLogger(LoggerConfiguration loggerConfiguration)
    {
        loggerConfiguration = loggerConfiguration.ReadFrom.Configuration(BootApplicationContext.Configuration);
        loggerConfiguration = loggerConfiguration
            .Enrich.FromLogContext()
            .Enrich.WithProperty("App", BootApplicationContext.Name)
            .Enrich.WithProperty("AppVersion", BootApplicationContext.Version)
            .Enrich.WithProperty("AppEnvironment", BootApplicationContext.Environment)
            .Enrich.WithMachineName();
        if (BootApplicationContext.Options.EnableConsoleLogging == true)
        {
            loggerConfiguration = loggerConfiguration.WriteTo.Console(
                outputTemplate: BootApplicationContext.Options.ConsoleLogFormat,
                formatProvider: CultureInfo.InvariantCulture);
        }

        return loggerConfiguration;
    }

    protected virtual void BeforeContainerBuild()
    {
        var enabledModules = ModulesHelper.GetEnabledModuleRegistrations(BootApplicationContext, moduleRegistrations);
        serilogConfigurator.ApplyLogging(BootApplicationContext, enabledModules);
        foreach (var moduleRegistration in enabledModules)
        {
            moduleRegistration.PostConfigureServices(BootApplicationContext, Services);
        }
    }

    protected void LogInternal(string message) => InternalLogger.LogInformation("Check log: {Message}", message);
}
