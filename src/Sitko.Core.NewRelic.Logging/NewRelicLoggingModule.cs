using NewRelic.LogEnrichers.Serilog;
using Serilog;
using Sitko.Core.App;
using Sitko.Core.App.Logging;

namespace Sitko.Core.NewRelic.Logging
{
    public class NewRelicLoggingModule : BaseApplicationModule<NewRelicLoggingModuleConfig>
    {
        public override string GetConfigKey()
        {
            return "Logging:NewRelic";
        }

        public override void ConfigureLogging(ApplicationContext context, NewRelicLoggingModuleConfig config,
            LoggerConfiguration loggerConfiguration, LogLevelSwitcher logLevelSwitcher)
        {
            base.ConfigureLogging(context, config, loggerConfiguration, logLevelSwitcher);
            if (config.EnableLogging)
            {
                loggerConfiguration
                    .Enrich.WithNewRelicLogsInContext()
                    .WriteTo.NewRelicLogs(config.LogsUrl,
                        context.Name,
                        config.LicenseKey);
            }
        }
    }
}
