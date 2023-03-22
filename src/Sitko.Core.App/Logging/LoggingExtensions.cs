using Microsoft.Extensions.Logging;
using Serilog;

namespace Sitko.Core.App.Logging;

public class LoggingExtensions
{
    public static void ConfigureSerilog(IApplicationContext appContext, ILoggingBuilder builder,
        Func<LoggerConfiguration, LoggerConfiguration> configureLogging)
    {
        var loggerConfiguration = new LoggerConfiguration();
        loggerConfiguration.ReadFrom.Configuration(appContext.Configuration);
        loggerConfiguration
            .Enrich.FromLogContext()
            .Enrich.WithProperty("App", appContext.Name)
            .Enrich.WithProperty("AppVersion", appContext.Version)
            .Enrich.WithProperty("AppEnvironment", appContext.Environment);
        loggerConfiguration = configureLogging(loggerConfiguration);
        Log.Logger = loggerConfiguration.CreateLogger();
        builder.ClearProviders();
        builder.AddSerilog();
    }
}
