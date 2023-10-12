using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Sitko.Core.App.Localization;
using Sitko.Core.App.Logging;
using Sitko.FluentValidation;
using Tempus;

namespace Sitko.Core.App;

public class SitkoCoreApplicationBuilder
{
    public static string OptionsKey => "Application";
    protected IApplicationContext BootApplicationContext { get; }
    protected IHostApplicationBuilder Builder { get; }

    private readonly List<ApplicationModuleRegistration> moduleRegistrations =
        new();

    public SitkoCoreApplicationBuilder(IHostApplicationBuilder builder, string[] args)
    {
        this.Builder = builder;
        var bootConfig = builder.Configuration.Build();
        var argsProvider = new ApplicationArgsProvider(args);
        BootApplicationContext = new BuilderApplicationContext(bootConfig, builder.Environment, argsProvider);

        builder.Configuration.Add(new SerilogDynamicConfigurationSource());

        builder.Services.AddSingleton<IApplicationArgsProvider>(argsProvider);
        builder.Services.AddSingleton<IApplicationContext, BuilderApplicationContext>();
        builder.Services.AddTransient<IScheduler, Scheduler>();
        builder.Services.AddFluentValidationExtensions();
        builder.Services.AddTransient(typeof(ILocalizationProvider<>), typeof(LocalizationProvider<>));
        builder.Services.AddHostedService<HostedLifecycleService>(); // только Hosted? Проверить для Wasm

        Console.OutputEncoding = Encoding.UTF8;
        var loggerConfiguration = new LoggerConfiguration();
        loggerConfiguration
            .WriteTo.Console(outputTemplate: ApplicationOptions.BaseConsoleLogFormat,
                formatProvider: CultureInfo.InvariantCulture,
                restrictedToMinimumLevel: LogEventLevel.Debug);
        InternalLogger = new SerilogLoggerFactory(loggerConfiguration.CreateLogger())
            .CreateLogger<SitkoCoreApplicationBuilder>();
        AddModule<CommandsModule>();
    }

    protected ILogger<SitkoCoreApplicationBuilder> InternalLogger { get; }

    protected Dictionary<string, LogEventLevel> LogEventLevels { get; } = new();

    protected List<Func<IApplicationContext, LoggerConfiguration, LoggerConfiguration>>
        LoggerConfigurationActions { get; } = new();

    // TODO: WHEN?
    private void ConfigureLogging()
    {
        LogInternal("Configure logging");
        LoggingExtensions.ConfigureSerilog(BootApplicationContext, Builder.Logging,
            configuration =>
            {
                configuration = configuration.Enrich.WithMachineName();
                if (BootApplicationContext.Options.EnableConsoleLogging == true)
                {
                    configuration = configuration.WriteTo.Console(
                        outputTemplate: BootApplicationContext.Options.ConsoleLogFormat,
                        formatProvider: CultureInfo.InvariantCulture);
                }

                return ConfigureLogging(BootApplicationContext, configuration);
            });
    }

    protected virtual LoggerConfiguration ConfigureLogging(IApplicationContext applicationContext,
        LoggerConfiguration loggerConfiguration)
    {
        foreach (var (key, value) in LogEventLevels)
        {
            loggerConfiguration = loggerConfiguration.MinimumLevel.Override(key, value);
        }

        foreach (var moduleRegistration in ModulesHelper.GetEnabledModuleRegistrations<ILoggingModule>(
                     applicationContext, moduleRegistrations))
        {
            loggerConfiguration = moduleRegistration.ConfigureLogging(applicationContext, loggerConfiguration);
        }

        foreach (var loggerConfigurationAction in LoggerConfigurationActions)
        {
            loggerConfiguration = loggerConfigurationAction(applicationContext, loggerConfiguration);
        }

        return loggerConfiguration;
    }

    protected void LogInternal(string message) =>
        InternalLogger.LogInformation("Check log: {Message}", message);

    public SitkoCoreApplicationBuilder AddModule<TModule>() where TModule : BaseApplicationModule, new()
    {
        RegisterModule<TModule, BaseApplicationModuleOptions>();
        return this;
    }

    public SitkoCoreApplicationBuilder AddModule<TModule, TModuleOptions>(
        Action<IApplicationContext, TModuleOptions> configureOptions,
        string? optionsKey = null)
        where TModule : IApplicationModule<TModuleOptions>, new()
        where TModuleOptions : BaseModuleOptions, new()
    {
        RegisterModule<TModule, TModuleOptions>(configureOptions, optionsKey);
        return this;
    }

    public SitkoCoreApplicationBuilder AddModule<TModule, TModuleOptions>(
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

    private bool CanAddModule() => moduleRegistrations.Count < 100500; // TODO: still needed?

    private void RegisterModule<TModule, TModuleOptions>(
        Action<IApplicationContext, TModuleOptions>? configureOptions = null,
        string? optionsKey = null)
        where TModule : IApplicationModule<TModuleOptions>, new() where TModuleOptions : BaseModuleOptions, new()
    {
        if (!CanAddModule())
        {
            throw new InvalidOperationException("App host is already built. Can't add modules after it");
        }

        var instance = new TModule();
        if (!instance.AllowMultiple && HasModule<TModule>())
        {
            throw new InvalidOperationException($"Module {typeof(TModule)} already registered");
        }

        var registration =
            new ApplicationModuleRegistration<TModule, TModuleOptions>(instance, configureOptions, optionsKey);

        if (typeof(TModule).IsAssignableTo(typeof(IHostBuilderModule)))
        {
            ConfigureHostBuilder<TModule, TModuleOptions>(registration);
        }

        registration.ConfigureAppConfiguration(BootApplicationContext, Builder.Configuration);
        registration.ConfigureOptions(BootApplicationContext, Builder.Services);
        registration.ConfigureServices(BootApplicationContext, Builder.Services);

        if (typeof(TModule).IsAssignableTo(typeof(IHostBuilderModule)))
        {
            registration.PostConfigureHostBuilder(BootApplicationContext, Builder);
        }

        moduleRegistrations.Add(registration);
        Builder.Services.AddSingleton<ApplicationModuleRegistration>(registration);
    }

    protected virtual void ConfigureHostBuilder<TModule, TModuleOptions>(ApplicationModuleRegistration registration)
        where TModule : IApplicationModule<TModuleOptions>, new() where TModuleOptions : BaseModuleOptions, new() =>
        registration.ConfigureHostBuilder(BootApplicationContext, Builder);

    public SitkoCoreApplicationBuilder ConfigureLogLevel(string source, LogEventLevel level)
    {
        LogEventLevels.Add(source, level);
        return this;
    }

    public SitkoCoreApplicationBuilder ConfigureLogging(
        Func<IApplicationContext, LoggerConfiguration, LoggerConfiguration> configure)
    {
        LoggerConfigurationActions.Add(configure);
        return this;
    }

    public SitkoCoreApplicationBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(Builder.Services);
        return this;
    }
}
