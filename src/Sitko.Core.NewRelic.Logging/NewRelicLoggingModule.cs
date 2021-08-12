using NewRelic.LogEnrichers.Serilog;
using Serilog;
using Sitko.Core.App;

namespace Sitko.Core.NewRelic.Logging
{
    public class NewRelicLoggingModule : BaseApplicationModule<NewRelicLoggingModuleOptions>
    {
        public override string OptionsKey => "Logging:NewRelic";

        public override void ConfigureLogging(ApplicationContext context, NewRelicLoggingModuleOptions options,
            LoggerConfiguration loggerConfiguration)
        {
            base.ConfigureLogging(context, options, loggerConfiguration);
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
