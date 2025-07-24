using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Sitko.Core.App;

public interface IApplicationEnvironment
{
    string EnvironmentName { get; }
    bool IsDevelopment();
    bool IsProduction();
    bool IsStaging();
    bool IsEnvironment(string environmentName);
}

public class ServerApplicationEnvironment : IApplicationEnvironment
{
    private readonly IHostEnvironment hostEnvironment;

    public ServerApplicationEnvironment(IHostEnvironment hostEnvironment) => this.hostEnvironment = hostEnvironment;

    public string EnvironmentName => hostEnvironment.EnvironmentName;
    public bool IsDevelopment() => hostEnvironment.IsDevelopment();

    public bool IsProduction() => hostEnvironment.IsProduction();
    public bool IsStaging() => hostEnvironment.IsStaging();
    public bool IsEnvironment(string environmentName) => hostEnvironment.IsEnvironment(environmentName);
}

public class BuilderApplicationContext : IApplicationContext
{
    private readonly IApplicationEnvironment environment;
    private readonly Func<List<ApplicationModuleRegistration>> getModuleRegistrations;

    private ApplicationOptions? applicationOptions;

    public BuilderApplicationContext(IConfiguration configuration, IApplicationEnvironment environment,
        IApplicationArgsProvider applicationArgsProvider,
        Func<List<ApplicationModuleRegistration>> getModuleRegistrations)
    {
        this.environment = environment;
        this.getModuleRegistrations = getModuleRegistrations;
        Configuration = configuration;
        var loggerConfiguration = new LoggerConfiguration();
        loggerConfiguration
            .WriteTo.Console(outputTemplate: ApplicationOptions.BaseConsoleLogFormat,
                formatProvider: CultureInfo.InvariantCulture,
                restrictedToMinimumLevel: LogEventLevel.Debug);
        Logger = new SerilogLoggerFactory(loggerConfiguration.CreateLogger()).CreateLogger<IApplicationContext>();
        Args = applicationArgsProvider.Args;
    }

    public static string OptionsKey => "Application";

    public ApplicationOptions Options => GetApplicationOptions();

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => Options.Name;
    public string Version => Options.Version;
    public string Environment => Options.Environment;

    public IConfiguration Configuration { get; }
    public ILogger Logger { get; }

    public string AspNetEnvironmentName => environment.EnvironmentName;
    public bool IsDevelopment() => environment.IsDevelopment();

    public bool IsProduction() => environment.IsProduction();
    public bool IsStaging() => environment.IsStaging();

    public bool IsEnvironment(string environmentName) => environment.IsEnvironment(environmentName);

    T IApplicationContext.GetModuleInstance<T>()
    {
        var registration = getModuleRegistrations().FirstOrDefault(x => x.Type == typeof(T));
        if (registration is null)
        {
            throw new InvalidOperationException($"Module {typeof(T).Name} is not registered");
        }

        var instance = registration.GetInstance();
        if (instance is T typedInstance)
        {
            return typedInstance;
        }

        throw new InvalidOperationException($"Instance of module {typeof(T).Name} is not of type {typeof(T).Name}");
    }

    public string[] Args { get; }

    private ApplicationOptions GetApplicationOptions()
    {
        if (applicationOptions is not null)
        {
            return applicationOptions;
        }

        applicationOptions = new ApplicationOptions();
        Configuration.Bind(OptionsKey, applicationOptions);
        if (string.IsNullOrEmpty(applicationOptions.Name))
        {
            applicationOptions.Name = Assembly.GetEntryAssembly()?.GetName().Name ?? "App";
        }

        if (string.IsNullOrEmpty(applicationOptions.Version))
        {
            applicationOptions.Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "dev";
        }

        if (string.IsNullOrEmpty(applicationOptions.Environment))
        {
            applicationOptions.Environment = AspNetEnvironmentName;
        }

        ConfigureApplicationOptions(applicationOptions);
        return applicationOptions;
    }

    protected void ConfigureApplicationOptions(ApplicationOptions options) =>
        options.EnableConsoleLogging ??= environment.IsDevelopment();
}
