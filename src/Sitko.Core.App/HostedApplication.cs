using System;
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
using Sitko.FluentValidation;
using Tempus;
using Thinktecture.Extensions.Configuration;

namespace Sitko.Core.App;

public abstract class HostedApplication : Application
{
    private IHost? appHost;

    protected HostedApplication(string[] args) : base(args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var loggerConfiguration = new LoggerConfiguration();
        loggerConfiguration
            .WriteTo.Console(outputTemplate: ApplicationOptions.BaseConsoleLogFormat,
                restrictedToMinimumLevel: LogEventLevel.Debug);
        InternalLogger = new SerilogLoggerFactory(loggerConfiguration.CreateLogger()).CreateLogger<Application>();
    }

    protected ILogger<Application> InternalLogger { get; }

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

    protected override void LogInternal(string message) =>
        InternalLogger.LogInformation("Check log: {Message}", message);

    protected IHost CreateAppHost(Action<IHostBuilder>? configure = null)
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

    protected virtual void ConfigureApplicationOptions(IHostEnvironment environment, IConfiguration configuration,
        ApplicationOptions options)
    {
    }


    protected IApplicationContext GetContext(IHostEnvironment environment, IConfiguration configuration) =>
        new HostedApplicationContext(configuration, environment);

    protected IHostBuilder ConfigureHostBuilder(Action<IHostBuilder>? configure = null)
    {
        LogInternal("Configure host builder start");

        LogInternal("Create tmp host builder");

        using var tmpHost = CreateHostBuilder(Args)
            .UseDefaultServiceProvider(options =>
            {
                options.ValidateOnBuild = false;
                options.ValidateScopes = true;
            })
            .ConfigureLogging(builder => { builder.SetMinimumLevel(LogLevel.Information); }).Build();

        var tmpConfiguration = tmpHost.Services.GetRequiredService<IConfiguration>();
        var tmpEnvironment = tmpHost.Services.GetRequiredService<IHostEnvironment>();

        var tmpApplicationContext = GetContext(tmpEnvironment, tmpConfiguration);

        LogInternal("Init application");

        InitApplication();

        LogInternal("Create main host builder");
        var serilogConfiguration = new SerilogConfiguration();
        var hostBuilder = CreateHostBuilder(Args)
            .UseDefaultServiceProvider(options =>
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            })
            .ConfigureHostConfiguration(builder =>
            {
                builder.AddJsonFile("appsettings.json", true, false);
                builder.AddJsonFile($"appsettings.{tmpApplicationContext.EnvironmentName}.json", true,
                    false);
            })
            .ConfigureAppConfiguration((context, builder) =>
            {
                LogInternal("Configure app configuration");
                var appContext = GetContext(context.HostingEnvironment, context.Configuration);
                foreach (var appConfigurationAction in AppConfigurationActions)
                {
                    appConfigurationAction(appContext, builder);
                }

                LoggingExtensions.ConfigureSerilogConfiguration(builder, serilogConfiguration);
                LogInternal("Configure app configuration in modules");
                foreach (var moduleRegistration in GetEnabledModuleRegistrations(tmpApplicationContext))
                {
                    moduleRegistration.ConfigureAppConfiguration(appContext, builder);
                }
            })
            .ConfigureServices((context, services) =>
            {
                LogInternal("Configure app services");
                services.AddSingleton(typeof(IApplication), this);
                services.AddSingleton(typeof(Application), this);
                services.AddSingleton(GetType(), this);
                services.AddSingleton<IApplicationContext, HostedApplicationContext>();
                services.AddHostedService<ApplicationLifetimeService>();
                services.AddTransient<IScheduler, Scheduler>();
                services.AddFluentValidationExtensions();
                services.AddTransient(typeof(ILocalizationProvider<>), typeof(LocalizationProvider<>));

                var appContext = GetContext(context.HostingEnvironment, context.Configuration);
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
                LogInternal("Configure logging");
                var appContext = GetContext(context.HostingEnvironment, context.Configuration);
                LoggingExtensions.ConfigureSerilog(appContext, builder, serilogConfiguration, configuration =>
                {
                    configuration.Enrich.WithMachineName();
                    if (appContext.Options.EnableConsoleLogging == true)
                    {
                        configuration.WriteTo.Console(outputTemplate: appContext.Options.ConsoleLogFormat);
                    }

                    ConfigureLogging(appContext, configuration);
                });
            });

        LogInternal("Configure host builder in modules");
        foreach (var moduleRegistration in GetEnabledModuleRegistrations(tmpApplicationContext))
        {
            moduleRegistration.ConfigureHostBuilder(tmpApplicationContext, hostBuilder);
        }

        LogInternal("Configure host builder");
        configure?.Invoke(hostBuilder);
        LogInternal("Create host builder done");
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

    protected override IApplicationContext GetContext() => appHost is not null
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

    protected override async Task<IApplicationContext> BuildAppContextAsync()
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

    protected override IApplicationContext GetContext(IServiceProvider serviceProvider) => GetContext(
        serviceProvider.GetRequiredService<IHostEnvironment>(),
        serviceProvider.GetRequiredService<IConfiguration>());
}

public class HostedApplicationContext : BaseApplicationContext
{
    private readonly IHostEnvironment environment;

    public HostedApplicationContext(IConfiguration configuration, IHostEnvironment environment) : base(
        configuration) =>
        this.environment = environment;

    public override string EnvironmentName => environment.EnvironmentName;
    public override bool IsDevelopment() => environment.IsDevelopment();

    public override bool IsProduction() => environment.IsDevelopment();

    protected override void ConfigureApplicationOptions(ApplicationOptions options) =>
        options.EnableConsoleLogging ??= environment.IsDevelopment();
}
