using JetBrains.Annotations;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Sitko.Core.App;
using Sitko.Core.App.Localization;
using Sitko.Core.Blazor.Components;
using Sitko.FluentValidation;
using Tempus;
using Thinktecture;
using Thinktecture.Extensions.Configuration;

namespace Sitko.Core.Blazor.Wasm;

public abstract class WasmApplication : Application
{
    private WebAssemblyHost? appHost;

    public WasmApplication(string[] args) : base(args)
    {
    }

    protected WebAssemblyHost CreateAppHost(Action<WebAssemblyHostBuilder>? configure = null)
    {
        LogInternal("Create app host start");

        if (appHost is not null)
        {
            LogInternal("App host is already built");

            return appHost;
        }

        LogInternal("Configure host builder");

        var hostBuilder = ConfigureHostBuilder(configure);

        LogInternal("Build host");
        var newHost = hostBuilder.Build();

        appHost = newHost;
        LogInternal("Create app host done");
        return appHost;
    }

    private WebAssemblyHostBuilder CreateHostBuilder(string[] hostBuilderArgs)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(hostBuilderArgs);
        ConfigureHostBuilder(builder);
        return builder;
    }

    protected abstract void ConfigureHostBuilder(WebAssemblyHostBuilder builder);

    private WebAssemblyHostBuilder ConfigureHostBuilder(Action<WebAssemblyHostBuilder>? configure = null)
    {
        LogInternal("Configure host builder start");

        LogInternal("Create tmp host builder");


        LogInternal("Init application");

        InitApplication();

        LogInternal("Create host builder");
        var hostBuilder = CreateHostBuilder(Args);
        var applicationContext = GetContext(hostBuilder.HostEnvironment, hostBuilder.Configuration);
        var enabledModuleRegistrations = GetEnabledModuleRegistrations(applicationContext);
        LogInternal("Configure app configuration");
        foreach (var appConfigurationAction in AppConfigurationActions)
        {
            appConfigurationAction(applicationContext, hostBuilder.Configuration);
        }

        LogInternal("Configure app configuration in modules");
        foreach (var moduleRegistration in enabledModuleRegistrations)
        {
            moduleRegistration.ConfigureAppConfiguration(applicationContext, hostBuilder.Configuration);
        }

        LogInternal("Configure app services");
        hostBuilder.Services.AddSingleton(typeof(IApplication), this);
        hostBuilder.Services.AddSingleton(typeof(Application), this);
        hostBuilder.Services.AddSingleton(GetType(), this);
        hostBuilder.Services.AddSingleton<IApplicationContext, WasmApplicationContext>();
        hostBuilder.Services.AddTransient<IScheduler, Scheduler>();
        hostBuilder.Services.AddFluentValidationExtensions();
        hostBuilder.Services.AddTransient(typeof(ILocalizationProvider<>), typeof(LocalizationProvider<>));
        hostBuilder.Services.AddScoped<CompressedPersistentComponentState>();
        foreach (var servicesConfigurationAction in ServicesConfigurationActions)
        {
            servicesConfigurationAction(applicationContext, hostBuilder.Services);
        }

        foreach (var moduleRegistration in enabledModuleRegistrations)
        {
            moduleRegistration.ConfigureOptions(applicationContext, hostBuilder.Services);
            moduleRegistration.ConfigureServices(applicationContext, hostBuilder.Services);
        }

        LogInternal("Configure logging");
        var loggerConfiguration = new LoggerConfiguration();
        loggerConfiguration.ReadFrom.Configuration(applicationContext.Configuration);
        loggerConfiguration
            .Enrich.FromLogContext()
            .Enrich.WithProperty("App", applicationContext.Name)
            .Enrich.WithProperty("AppVersion", applicationContext.Version).WriteTo
            .BrowserConsole(
                outputTemplate:
                "{Level:u3}{SourceContext}{Message:lj}{NewLine}{Exception}");

        var loggingConfiguration = new SerilogConfiguration();
        hostBuilder.Configuration.AddLoggingConfiguration(loggingConfiguration, "Serilog");
        hostBuilder.Services.AddSingleton<ISerilogConfiguration>(loggingConfiguration);
        ConfigureLogging(applicationContext, loggerConfiguration);
        foreach (var (key, value) in LogEventLevels)
        {
            loggerConfiguration.MinimumLevel.Override(key, value);
        }

        foreach (var moduleRegistration in enabledModuleRegistrations)
        {
            moduleRegistration.ConfigureLogging(applicationContext, loggerConfiguration);
        }

        foreach (var loggerConfigurationAction in LoggerConfigurationActions)
        {
            loggerConfigurationAction(applicationContext, loggerConfiguration);
        }

        Log.Logger = loggerConfiguration.CreateLogger();
        hostBuilder.Logging.ClearProviders();
        hostBuilder.Logging.AddSerilog();

        LogInternal("Configure host builder in modules");
        foreach (var configurationModule in enabledModuleRegistrations
                     .Select(module => module.GetInstance())
                     .OfType<IWasmApplicationModule>())
        {
            configurationModule.ConfigureHostBuilder(applicationContext, hostBuilder);
        }

        LogInternal("Configure host builder");
        configure?.Invoke(hostBuilder);
        LogInternal("Create host builder done");
        return hostBuilder;
    }

    protected override void LogInternal(string message) => Log.Logger.Debug("Internal: {Message}", message);

    protected async Task<WebAssemblyHost> GetOrCreateHostAsync(Action<WebAssemblyHostBuilder>? configure = null)
    {
        if (appHost is not null)
        {
            return appHost;
        }

        appHost = CreateAppHost(configure);

        await InitAsync(appHost.Services);

        return appHost;
    }

    protected override async Task DoRunAsync()
    {
        var currentHost = await GetOrCreateHostAsync();
        await currentHost.RunAsync();
    }

    protected override async Task<IApplicationContext> BuildAppContextAsync()
    {
        var currentHost = await GetOrCreateHostAsync();
        return GetContext(currentHost.Services);
    }

    public override Task StopAsync() => throw new NotImplementedException();
    protected override bool CanAddModule() => true;

    protected override IApplicationContext GetContext() => appHost is not null
        ? GetContext(appHost.Services)
        : throw new InvalidOperationException("App host is not built yet");

    [PublicAPI]
    protected static IApplicationContext GetContext(IWebAssemblyHostEnvironment environment,
        IConfiguration configuration) =>
        new WasmApplicationContext(configuration, environment);

    protected override IApplicationContext GetContext(IServiceProvider serviceProvider) => GetContext(
        serviceProvider.GetRequiredService<IWebAssemblyHostEnvironment>(),
        serviceProvider.GetRequiredService<IConfiguration>());
}
