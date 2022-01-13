using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Sitko.Core.App.Localization;
using Sitko.FluentValidation;
using Tempus;
using Thinktecture;
using Thinktecture.Extensions.Configuration;

namespace Sitko.Core.App;

public abstract class HostedApplication : Application
{
    private IHost? appHost;

    protected HostedApplication(string[] args) : base(args)
    {
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
        var newHost = hostBuilder.Build();

        appHost = newHost;
        LogVerbose("Create app host done");
        return appHost;
    }

    protected IHostBuilder ConfigureHostBuilder(Action<IHostBuilder>? configure = null)
    {
        LogVerbose("Configure host builder start");

        LogVerbose("Create tmp host builder");

        using var tmpHost = CreateHostBuilder(Args)
            .UseDefaultServiceProvider(options =>
            {
                options.ValidateOnBuild = false;
                options.ValidateScopes = true;
            })
            .ConfigureLogging(builder => { builder.SetMinimumLevel(LogLevel.Information); }).Build();

        var tmpConfiguration = tmpHost.Services.GetRequiredService<IConfiguration>();
        var tmpEnvironment = new HostedAppEnvironment(tmpHost.Services.GetRequiredService<IHostEnvironment>());

        var tmpApplicationContext = GetContext(tmpEnvironment, tmpConfiguration);

        LogVerbose("Init application");

        InitApplication();

        LogVerbose("Create main host builder");
        var loggingConfiguration = new SerilogConfiguration();
        var hostBuilder = CreateHostBuilder(Args)
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
                var appContext = GetContext(new HostedAppEnvironment(context.HostingEnvironment),
                    context.Configuration);
                foreach (var appConfigurationAction in AppConfigurationActions)
                {
                    appConfigurationAction(appContext, builder);
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
                services.AddFluentValidationExtensions();
                services.AddTransient(typeof(ILocalizationProvider<>), typeof(LocalizationProvider<>));

                var appContext = GetContext(new HostedAppEnvironment(context.HostingEnvironment),
                    context.Configuration);
                foreach (var servicesConfigurationAction in ServicesConfigurationActions)
                {
                    servicesConfigurationAction(appContext, services);
                }

                foreach (var moduleRegistration in GetEnabledModuleRegistrations(appContext))
                {
                    moduleRegistration.ConfigureOptions(appContext, services);
                    moduleRegistration.ConfigureServices(appContext, services);
                }
            }).ConfigureLogging((context, builder) =>
            {
                var applicationOptions = GetApplicationOptions(new HostedAppEnvironment(context.HostingEnvironment),
                    context.Configuration);
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

                var appContext = GetContext(new HostedAppEnvironment(context.HostingEnvironment),
                    context.Configuration);
                ConfigureLogging(appContext,
                    loggerConfiguration);
                foreach (var (key, value) in
                         LogEventLevels)
                {
                    loggerConfiguration.MinimumLevel.Override(key, value);
                }

                foreach (var moduleRegistration in GetEnabledModuleRegistrations(appContext))
                {
                    moduleRegistration.ConfigureLogging(tmpApplicationContext, loggerConfiguration);
                }

                foreach (var loggerConfigurationAction in LoggerConfigurationActions)
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

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);
        if (disposing)
        {
            appHost?.Dispose();
        }
    }

    public async Task ExecuteAsync(Func<IServiceProvider, Task> command)
    {
        var currentHost = await GetOrCreateHostAsync(builder => builder.UseConsoleLifetime());

        var serviceProvider = currentHost.Services;

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


    public override Task StopAsync() => CreateAppHost().StopAsync();
    protected override bool CanAddModule() => appHost is null;
    public IHostBuilder GetHostBuilder() => ConfigureHostBuilder();

    protected override ApplicationContext GetContext() => appHost is not null
        ? GetContext(appHost.Services)
        : throw new InvalidOperationException("App host is not built yet");

    protected async Task<IHost> GetOrCreateHostAsync(Action<IHostBuilder>? configure = null)
    {
        if (appHost is not null)
        {
            return appHost;
        }

        appHost = CreateAppHost(configure);

        await InitAsync(appHost.Services);

        return appHost;
    }

    protected override async Task<ApplicationContext> BuildAppContextAsync()
    {
        var currentHost = await GetOrCreateHostAsync();
        return GetContext(currentHost.Services);
    }

    public async Task<IHost> StartAsync()
    {
        var currentHost = await GetOrCreateHostAsync();
        await currentHost.StartAsync();
        return currentHost;
    }

    protected override async Task DoRunAsync()
    {
        var currentHost = await GetOrCreateHostAsync();
        await currentHost.RunAsync();
    }

    public async Task<IServiceProvider> GetServiceProviderAsync() => (await GetOrCreateHostAsync()).Services;

    protected override ApplicationContext GetContext(IServiceProvider serviceProvider) => GetContext(
        new HostedAppEnvironment(serviceProvider.GetRequiredService<IHostEnvironment>()),
        serviceProvider.GetRequiredService<IConfiguration>(),
        serviceProvider.GetRequiredService<ILogger<Application>>());
}

public class HostedAppEnvironment : IAppEnvironment
{
    private readonly IHostEnvironment environment;

    public HostedAppEnvironment(IHostEnvironment environment) => this.environment = environment;

    public string EnvironmentName => environment.EnvironmentName;
    public string ApplicationName => environment.ApplicationName;
    public bool IsDevelopment() => environment.IsDevelopment();
    public bool IsProduction() => environment.IsProduction();
}
