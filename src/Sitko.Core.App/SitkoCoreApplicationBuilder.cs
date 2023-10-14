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

    private readonly SerilogConfigurator serilogConfigurator = new();

    public SitkoCoreApplicationBuilder(IHostApplicationBuilder builder, string[] args)
    {
        Builder = builder;
        var bootConfig = builder.Configuration.Build();
        var argsProvider = new ApplicationArgsProvider(args);
        BootApplicationContext = new BuilderApplicationContext(bootConfig, builder.Environment, argsProvider);

        // configure logging
        builder.Configuration.Add(new SerilogDynamicConfigurationSource());
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
        InternalLogger = new SerilogLoggerFactory(tmpLogger)
            .CreateLogger<SitkoCoreApplicationBuilder>();
        AddModule<CommandsModule>();
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        serilogConfigurator.Configure(configuration => ConfigureDefautLogger(configuration));


        builder.Services.AddSingleton<IApplicationArgsProvider>(argsProvider);
        builder.Services.AddSingleton(serilogConfigurator);
        builder.Services.AddSingleton<IApplicationContext, BuilderApplicationContext>();
        builder.Services.AddTransient<IScheduler, Scheduler>();
        builder.Services.AddFluentValidationExtensions();
        builder.Services.AddTransient(typeof(ILocalizationProvider<>), typeof(LocalizationProvider<>));
        builder.Services.AddHostedService<HostedLifecycleService>(); // только Hosted? Проверить для Wasm
    }

    protected ILogger<SitkoCoreApplicationBuilder> InternalLogger { get; }

    protected LoggerConfiguration ConfigureDefautLogger(LoggerConfiguration loggerConfiguration)
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
        }

        moduleRegistrations.Add(registration);
        Builder.Services.AddSingleton<ApplicationModuleRegistration>(registration);
    }

    protected virtual void ConfigureHostBuilder<TModule, TModuleOptions>(ApplicationModuleRegistration registration)
        where TModule : IApplicationModule<TModuleOptions>, new() where TModuleOptions : BaseModuleOptions, new() =>
        registration.ConfigureHostBuilder(BootApplicationContext, Builder);

    public SitkoCoreApplicationBuilder ConfigureLogLevel(string source, LogEventLevel level)
    {
        serilogConfigurator.ConfigureLogLevel(source, level);
        return this;
    }

    public SitkoCoreApplicationBuilder ConfigureLogging(
        Func<IApplicationContext, LoggerConfiguration, LoggerConfiguration> configure)
    {
        serilogConfigurator.ConfigureLogging(configure);
        return this;
    }

    public SitkoCoreApplicationBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(Builder.Services);
        return this;
    }
}
