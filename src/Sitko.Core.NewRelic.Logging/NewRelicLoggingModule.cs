using NewRelic.LogEnrichers.Serilog;
using Serilog;
using Sitko.Core.App;

namespace Sitko.Core.NewRelic.Logging
{
    public class NewRelicLoggingModule : BaseApplicationModule<NewRelicLoggingModuleOptions>,
        ILoggingModule<NewRelicLoggingModuleOptions>
    {
        public override string OptionsKey => "Logging:NewRelic";

        public void ConfigureLogging(ApplicationContext context, NewRelicLoggingModuleOptions options,
            LoggerConfiguration loggerConfiguration)
        {
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
