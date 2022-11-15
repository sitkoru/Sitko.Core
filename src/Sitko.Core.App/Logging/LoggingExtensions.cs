using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Thinktecture;
using Thinktecture.Extensions.Configuration;

namespace Sitko.Core.App.Logging;

public class LoggingExtensions
{
    public static void ConfigureSerilogConfiguration(IConfigurationBuilder configurationBuilder,
        SerilogConfiguration serilogConfiguration) =>
        configurationBuilder.AddLoggingConfiguration(serilogConfiguration, "Serilog");

    public static void ConfigureSerilog(IApplicationContext appContext, ILoggingBuilder builder,
        SerilogConfiguration serilogConfiguration, Func<LoggerConfiguration, LoggerConfiguration> configureLogging)
    {
        builder.Services.AddSingleton<ISerilogConfiguration>(serilogConfiguration);
        builder.AddConfiguration(appContext.Configuration.GetSection("Logging"));
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

