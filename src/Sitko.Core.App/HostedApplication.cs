using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Sitko.Core.App.Helpers;
using Sitko.Core.App.Logging;

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
                formatProvider: CultureInfo.InvariantCulture,
                restrictedToMinimumLevel: LogEventLevel.Debug);
        InternalLogger = new SerilogLoggerFactory(loggerConfiguration.CreateLogger()).CreateLogger<Application>();
    }

    protected ILogger<Application> InternalLogger { get; }

    protected virtual IHostBuilder CreateHostBuilderBase(string[] hostBuilderArgs) =>
        Host.CreateDefaultBuilder(hostBuilderArgs);

    private IHostBuilder CreateHostBuilder(string[] hostBuilderArgs)
    {
        var builder = CreateHostBuilderBase(hostBuilderArgs);
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

    protected IApplicationContext GetContext(IHostEnvironment environment, IConfiguration configuration) =>
        new HostedApplicationContext(this, configuration, environment);

    protected IHostBuilder ConfigureHostBuilder(Action<IHostBuilder>? configure = null)
    {
        LogInternal("Configure host builder start");

        LogInternal("Create tmp host builder");

        var startEnvironment = new HostingEnvironment
        {
            ApplicationName = GetType().Assembly.FullName!, EnvironmentName = EnvHelper.GetEnvironmentName()
        };

        var configBuilder = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddEnvironmentVariables("DOTNET_")
            .AddEnvironmentVariables("ASPNETCORE_")
            .AddJsonFile("appsettings.json", true, false)
            .AddJsonFile($"appsettings.{startEnvironment.EnvironmentName}.json", true, false);
        var startApplicationContext = GetContext(startEnvironment, configBuilder.Build());
        ConfigureConfiguration(startApplicationContext, configBuilder);

        LogInternal("Init application");

        InitApplication();

        LogInternal("Create main host builder");
        var hostBuilder = CreateHostBuilder(Args)
            .UseDefaultServiceProvider(options =>
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            });

        LogInternal("Configure host builder in modules");
        var bootConfiguration = configBuilder.Build();
        var bootEnvironment = new HostingEnvironment
        {
            ApplicationName = bootConfiguration[HostDefaults.ApplicationKey] ?? "",
            EnvironmentName = bootConfiguration[HostDefaults.EnvironmentKey] ?? Environments.Production
        };
        var bootApplicationContext = GetContext(bootEnvironment, bootConfiguration);

        foreach (var moduleRegistration in GetEnabledModuleRegistrations<IHostBuilderModule>(bootApplicationContext))
        {
            moduleRegistration.ConfigureHostBuilder(bootApplicationContext, hostBuilder);
        }

        LogInternal("Configure host builder");
        hostBuilder.ConfigureAppConfiguration((_, builder) =>
            {
                ConfigureConfiguration(bootApplicationContext, builder);
            })
            .ConfigureServices((_, services) =>
            {
                RegisterApplicationServices<HostedApplicationContext>(bootApplicationContext, services);
                services.AddHostedService<ApplicationLifetimeService>();
            }).ConfigureLogging((builderContext, builder) =>
            {
                LogInternal("Configure logging");
                var runtimeContext = GetContext(builderContext.HostingEnvironment, builderContext.Configuration);
                LoggingExtensions.ConfigureSerilog(runtimeContext, builder,
                    configuration =>
                    {
                        configuration = configuration.Enrich.WithMachineName();
                        if (runtimeContext.Options.EnableConsoleLogging == true)
                        {
                            configuration = configuration.WriteTo.Console(
                                outputTemplate: runtimeContext.Options.ConsoleLogFormat,
                                formatProvider: CultureInfo.InvariantCulture);
                        }

                        return ConfigureLogging(runtimeContext, configuration);
                    });
            });
        configure?.Invoke(hostBuilder);
        PostConfigureHostBuilder(bootApplicationContext, hostBuilder);
        LogInternal("Create host builder done");
        return hostBuilder;
    }

    protected virtual void PostConfigureHostBuilder(IApplicationContext applicationContext, IHostBuilder hostBuilder)
    {
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

    public HostedApplicationContext(Application application, IConfiguration configuration, IHostEnvironment environment)
        : base(application,
            configuration) =>
        this.environment = environment;

    public override string AspNetEnvironmentName => environment.EnvironmentName;
    public override bool IsDevelopment() => environment.IsDevelopment();

    public override bool IsProduction() => environment.IsProduction();

    protected override void ConfigureApplicationOptions(ApplicationOptions options) =>
        options.EnableConsoleLogging ??= environment.IsDevelopment();
}
