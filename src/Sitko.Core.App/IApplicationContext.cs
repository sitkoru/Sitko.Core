using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Sitko.Core.App;

public interface IApplicationContext
{
    string Name { get; }
    string Version { get; }
    ApplicationOptions Options { get; }
    IConfiguration Configuration { get; }
    ILogger Logger { get; }
    string EnvironmentName { get; }
    bool IsDevelopment();
    bool IsProduction();
}

public abstract class BaseApplicationContext : IApplicationContext
{
    private ApplicationOptions? applicationOptions;

    protected BaseApplicationContext(IConfiguration configuration)
    {
        Configuration = configuration;
        var loggerConfiguration = new LoggerConfiguration();
        loggerConfiguration
            .WriteTo.Console(outputTemplate: ApplicationOptions.BaseConsoleLogFormat,
                restrictedToMinimumLevel: LogEventLevel.Debug);
        Logger = new SerilogLoggerFactory(loggerConfiguration.CreateLogger()).CreateLogger<IApplicationContext>();
    }

    public ApplicationOptions Options => GetApplicationOptions();

    public string Name => Options.Name;
    public string Version => Options.Version;

    public IConfiguration Configuration { get; }
    public ILogger Logger { get; }
    public abstract string EnvironmentName { get; }
    public abstract bool IsDevelopment();

    public abstract bool IsProduction();

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
            applicationOptions.Name = GetType().Assembly.GetName().Name ?? "App";
        }

        if (string.IsNullOrEmpty(applicationOptions.Version))
        {
            applicationOptions.Version = GetType().Assembly.GetName().Version?.ToString() ?? "dev";
        }

        ConfigureApplicationOptions(applicationOptions);
        return applicationOptions;
    }

    protected virtual void ConfigureApplicationOptions(ApplicationOptions options)
    {
    }
}
