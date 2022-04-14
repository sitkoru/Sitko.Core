using JetBrains.Annotations;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Serilog;
using Sitko.Blazor.ScriptInjector;
using Sitko.Core.App;
using Sitko.Core.App.Logging;
using Sitko.Core.Blazor.Components;
using Thinktecture.Extensions.Configuration;
#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace Sitko.Core.Blazor.Wasm;

public abstract class WasmApplication : Application
{
    private WebAssemblyHost? appHost;

    protected WasmApplication(string[] args) : base(args)
    {
#if NET6_0_OR_GREATER
        this.AddPersistentState<JsonHelperStateCompressor, CompressedPersistentComponentState>();
#endif
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
        LogInternal("Init application");
        InitApplication();

        LogInternal("Create host builder");
        var hostBuilder = CreateHostBuilder(Args);
        var tmpHost = hostBuilder.Build();
        var applicationContext = GetContext(hostBuilder.HostEnvironment, hostBuilder.Configuration);
        var enabledModuleRegistrations = GetEnabledModuleRegistrations(applicationContext);
        // App configuration
        ConfigureConfiguration(applicationContext, hostBuilder.Configuration);
        // App services
        RegisterApplicationServices<WasmApplicationContext>(applicationContext, hostBuilder.Services);
        hostBuilder.Services.AddScriptInjector();
        hostBuilder.Services.AddScoped<CompressedPersistentComponentState>();
        // Logging
        LogInternal("Configure logging");
        var serilogConfiguration = new SerilogConfiguration();
        LoggingExtensions.ConfigureSerilogConfiguration(hostBuilder.Configuration, serilogConfiguration);
        LoggingExtensions.ConfigureSerilog(applicationContext, hostBuilder.Logging, serilogConfiguration,
            configuration =>
            {
                configuration.WriteTo.BrowserConsole(
                    outputTemplate: applicationContext.Options.ConsoleLogFormat,
                    jsRuntime: tmpHost.Services.GetRequiredService<IJSRuntime>());

                ConfigureLogging(applicationContext, configuration);
            });

        // Host builder via modules
        LogInternal("Configure host builder in modules");
        foreach (var configurationModule in enabledModuleRegistrations
                     .Select(module => module.GetInstance())
                     .OfType<IWasmApplicationModule>())
        {
            configurationModule.ConfigureHostBuilder(applicationContext, hostBuilder);
        }

        // Host builder via action
        LogInternal("Configure host builder");
        configure?.Invoke(hostBuilder);
        LogInternal("Create host builder done");
        return hostBuilder;
    }

    protected override void LogInternal(string message) => Log.Logger.Debug("Internal: {Message}", message);

    private async Task<WebAssemblyHost> GetOrCreateHostAsync(Action<WebAssemblyHostBuilder>? configure = null)
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
    protected IApplicationContext GetContext(IWebAssemblyHostEnvironment environment,
        IConfiguration configuration) =>
        new WasmApplicationContext(this, configuration, environment);

    protected override IApplicationContext GetContext(IServiceProvider serviceProvider) => GetContext(
        serviceProvider.GetRequiredService<IWebAssemblyHostEnvironment>(),
        serviceProvider.GetRequiredService<IConfiguration>());
}
