using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Sitko.Core.App;

public interface IApplicationContext
{
    Guid Id { get; }
    string Name { get; }
    string Version { get; }
    ApplicationOptions Options { get; }
    IConfiguration Configuration { get; }
    ILogger Logger { get; }
    string Environment { get; }
    string AspNetEnvironmentName { get; }
    bool IsDevelopment();
    bool IsProduction();
    public string[] Args { get; }
}

public abstract class BaseApplicationContext : IApplicationContext
{
    private readonly Application application;
    private ApplicationOptions? applicationOptions;

    protected BaseApplicationContext(Application application, IConfiguration configuration)
    {
        this.application = application;
        Configuration = configuration;
        var loggerConfiguration = new LoggerConfiguration();
        loggerConfiguration
            .WriteTo.Console(outputTemplate: ApplicationOptions.BaseConsoleLogFormat,
                formatProvider: CultureInfo.InvariantCulture,
                restrictedToMinimumLevel: LogEventLevel.Debug);
        Logger = new SerilogLoggerFactory(loggerConfiguration.CreateLogger()).CreateLogger<IApplicationContext>();
    }

    public ApplicationOptions Options => GetApplicationOptions();

    public Guid Id { get; } = Guid.NewGuid();
    public string Name => Options.Name;
    public string Version => Options.Version;
    public string Environment => Options.Environment;

    public IConfiguration Configuration { get; }
    public ILogger Logger { get; }
    public abstract string AspNetEnvironmentName { get; }
    public abstract bool IsDevelopment();

    public abstract bool IsProduction();
    public string[] Args { get; }

    private ApplicationOptions GetApplicationOptions()
    {
        if (applicationOptions is not null)
        {
            return applicationOptions;
        }

        applicationOptions = new ApplicationOptions();
        Configuration.Bind(Application.OptionsKey, applicationOptions);
        if (string.IsNullOrEmpty(applicationOptions.Name))
        {
            applicationOptions.Name = application.GetType().Assembly.GetName().Name ?? "App";
        }

        if (string.IsNullOrEmpty(applicationOptions.Version))
        {
            applicationOptions.Version = application.GetType().Assembly.GetName().Version?.ToString() ?? "dev";
        }

        if (string.IsNullOrEmpty(applicationOptions.Environment))
        {
            applicationOptions.Environment = AspNetEnvironmentName;
        }

        ConfigureApplicationOptions(applicationOptions);
        return applicationOptions;
    }

    protected virtual void ConfigureApplicationOptions(ApplicationOptions options)
    {
    }
}

