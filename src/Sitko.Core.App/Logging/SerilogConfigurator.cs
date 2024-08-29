using Serilog;
using Serilog.Events;

namespace Sitko.Core.App.Logging;

internal class SerilogConfigurator
{
    private LoggerConfiguration loggerConfiguration = new();

    private readonly List<Func<IApplicationContext, LoggerConfiguration, LoggerConfiguration>>
        loggerConfigurationActions = new();

    private readonly Dictionary<string, LogEventLevel> logEventLevels = new();

    public SerilogConfigurator Configure(Func<LoggerConfiguration, LoggerConfiguration> configure)
    {
        loggerConfiguration = configure(loggerConfiguration);
        return this;
    }

    private void RecreateLogger() => Log.Logger = loggerConfiguration.CreateLogger();

    public SerilogConfigurator ConfigureLogLevel(string source, LogEventLevel level)
    {
        logEventLevels[source] = level;
        return this;
    }

    public SerilogConfigurator ConfigureLogging(
        Func<IApplicationContext, LoggerConfiguration, LoggerConfiguration> configure)
    {
        loggerConfigurationActions.Add(configure);
        return this;
    }

    public void ApplyLogging(IApplicationContext applicationContext,
        IEnumerable<ApplicationModuleRegistration> enabledModules)
    {
        foreach (var (key, value) in logEventLevels)
        {
            loggerConfiguration = loggerConfiguration.MinimumLevel.Override(key, value);
        }

        foreach (var moduleRegistration in ModulesHelper.GetEnabledModuleRegistrations<ILoggingModule>(
                     applicationContext, enabledModules))
        {
            loggerConfiguration = moduleRegistration.ConfigureLogging(applicationContext, loggerConfiguration);
        }

        foreach (var loggerConfigurationAction in loggerConfigurationActions)
        {
            loggerConfiguration = loggerConfigurationAction(applicationContext, loggerConfiguration);
        }

        RecreateLogger();
    }
}
