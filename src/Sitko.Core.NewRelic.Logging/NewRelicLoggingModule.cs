using NewRelic.LogEnrichers.Serilog;
using Serilog;
using Sitko.Core.App;
using Sitko.Core.App.Logging;

namespace Sitko.Core.NewRelic.Logging
{
    public class NewRelicLoggingModule : BaseApplicationModule<NewRelicLoggingModuleOptions>
    {
        public override string OptionsKey => "Logging:NewRelic";

        public override void ConfigureLogging(ApplicationContext context, NewRelicLoggingModuleOptions options,
            LoggerConfiguration loggerConfiguration, LogLevelSwitcher logLevelSwitcher)
        {
            base.ConfigureLogging(context, options, loggerConfiguration, logLevelSwitcher);
            if (options.EnableLogging)
            {
                loggerConfiguration
                    .Enrich.WithNewRelicLogsInContext()
                    .WriteTo.NewRelicLogs(options.LogsUrl,
                        context.Name,
                        options.LicenseKey);
            }
        }
    }
}
