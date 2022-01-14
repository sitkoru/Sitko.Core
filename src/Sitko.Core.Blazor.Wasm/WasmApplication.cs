using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Sitko.Core.App;
using Sitko.Core.App.Localization;
using Sitko.FluentValidation;
using Tempus;
using Thinktecture.Extensions.Configuration;

namespace Sitko.Core.Blazor.Wasm;

public class WasmApplication<TApp> : Application where TApp : IComponent
{
    private WebAssemblyHost? appHost;

    public WasmApplication(string[] args) : base(args)
    {
    }

    protected WebAssemblyHost CreateAppHost(Action<WebAssemblyHostBuilder>? configure = null)
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
        var newHost = hostBuilder.Build();

        appHost = newHost;
        LogVerbose("Create app host done");
        return appHost;
    }

    private WebAssemblyHostBuilder CreateHostBuilder(string[] hostBuilderArgs)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(hostBuilderArgs);
        ConfigureHostBuilder(builder);
        return builder;
    }

    protected virtual void ConfigureHostBuilder(WebAssemblyHostBuilder builder)
    {
        builder.RootComponents.Add<TApp>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");
    }

    private WebAssemblyHostBuilder ConfigureHostBuilder(Action<WebAssemblyHostBuilder>? configure = null)
    {
        LogVerbose("Configure host builder start");

        LogVerbose("Create tmp host builder");

        var tmpHostBuilder = CreateHostBuilder(Args);
        // .UseDefaultServiceProvider(options =>
        // {
        //     options.ValidateOnBuild = false;
        //     options.ValidateScopes = true;
        // })
        // .ConfigureLogging(builder => { builder.SetMinimumLevel(LogLevel.Information); }).Build();
        tmpHostBuilder.Logging.SetMinimumLevel(LogLevel.Information);
        //tmpHostBuilder.ConfigureContainer(options => { });;
        var tmpHost = tmpHostBuilder.Build();
        var tmpApplicationContext =
            GetContext(tmpHostBuilder.HostEnvironment, tmpHost.Configuration);

        LogVerbose("Init application");

        InitApplication();

        LogVerbose("Create main host builder");
        var loggingConfiguration = new SerilogConfiguration();
        var hostBuilder = CreateHostBuilder(Args);
        // TODO: appsettings?
        // var hostBuilder = CreateHostBuilder(Args)
        LogVerbose("Configure app configuration");
        var tmpContext = GetContext(tmpHost.Services);
        foreach (var appConfigurationAction in AppConfigurationActions)
        {
            appConfigurationAction(tmpContext, hostBuilder.Configuration);
        }

        LogVerbose("Configure app configuration in modules");
        foreach (var moduleRegistration in GetEnabledModuleRegistrations(tmpApplicationContext))
        {
            moduleRegistration.ConfigureAppConfiguration(tmpContext, hostBuilder.Configuration);
        }

        LogVerbose("Configure app services");
        hostBuilder.Services.AddSingleton<ISerilogConfiguration>(loggingConfiguration);
        hostBuilder.Services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory());
        hostBuilder.Services.AddSingleton(typeof(IApplication), this);
        hostBuilder.Services.AddSingleton(typeof(Application), this);
        hostBuilder.Services.AddSingleton(GetType(), this);
        hostBuilder.Services.AddSingleton<IApplicationContext, WasmApplicationContext>();
        //hostBuilder.Services.AddHostedService<ApplicationLifetimeService>();
        hostBuilder.Services.AddTransient<IScheduler, Scheduler>();
        hostBuilder.Services.AddFluentValidationExtensions();
        hostBuilder.Services.AddTransient(typeof(ILocalizationProvider<>), typeof(LocalizationProvider<>));
        foreach (var servicesConfigurationAction in ServicesConfigurationActions)
        {
            servicesConfigurationAction(tmpContext, hostBuilder.Services);
        }

        foreach (var moduleRegistration in GetEnabledModuleRegistrations(tmpContext))
        {
            moduleRegistration.ConfigureOptions(tmpContext, hostBuilder.Services);
            moduleRegistration.ConfigureServices(tmpContext, hostBuilder.Services);
        }

        LogVerbose("Configure logging");
        //hostBuilder.Configuration.AddConfiguration(tmpContext.Configuration.GetSection("Logging"));
        var loggerConfiguration = new LoggerConfiguration();
        loggerConfiguration.ReadFrom.Configuration(tmpContext.Configuration);
        loggerConfiguration
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("App", tmpContext.Name)
            .Enrich.WithProperty("AppVersion", tmpContext.Version);
        //loggerConfiguration.WriteTo.BrowserConsole();

        //hostBuilder.Configuration.AddLoggingConfiguration(loggingConfiguration, "Serilog");
        ConfigureLogging(tmpContext, loggerConfiguration);
        foreach (var (key, value) in
                 LogEventLevels)
        {
            loggerConfiguration.MinimumLevel.Override(key, value);
        }

        foreach (var moduleRegistration in GetEnabledModuleRegistrations(tmpContext))
        {
            moduleRegistration.ConfigureLogging(tmpApplicationContext, loggerConfiguration);
        }

        foreach (var loggerConfigurationAction in LoggerConfigurationActions)
        {
            loggerConfigurationAction(tmpContext, loggerConfiguration);
        }

        Log.Logger = loggerConfiguration.CreateLogger();
        hostBuilder.Logging.AddSerilog();

        LogVerbose("Configure host builder in modules");
        // foreach (var moduleRegistration in GetEnabledModuleRegistrations(tmpApplicationContext))
        // {
        //     moduleRegistration.ConfigureHostBuilder(tmpApplicationContext, hostBuilder);
        // }

        LogVerbose("Configure host builder");
        configure?.Invoke(hostBuilder);
        LogVerbose("Create host builder done");
        return hostBuilder;
    }

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

    protected IApplicationContext GetContext(IWebAssemblyHostEnvironment environment, IConfiguration configuration) =>
        new WasmApplicationContext(configuration, environment);

    protected override IApplicationContext GetContext(IServiceProvider serviceProvider) => GetContext(
        serviceProvider.GetRequiredService<IWebAssemblyHostEnvironment>(),
        serviceProvider.GetRequiredService<IConfiguration>());
}
