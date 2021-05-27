using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Sinks.Graylog;
using Sitko.Core.App;
using Sitko.Core.App.Logging;

namespace Sitko.Core.Graylog
{
    public class GraylogModule : BaseApplicationModule<GraylogLoggingOptions>
    {
        public override string GetConfigKey()
        {
            return "Logging:Graylog";
        }

        public override void ConfigureLogging(ApplicationContext context, GraylogLoggingOptions config,
            LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher)
        {
            base.ConfigureLogging(context, config, loggerConfiguration, logLevelSwitcher);
            loggerConfiguration.WriteTo.Async(to => to.Graylog(
                new GraylogSinkOptions
                {
                    HostnameOrAddress = config.Host,
                    Port = config.Port,
                    Facility = context.Name,
                    MinimumLogEventLevel = logLevelSwitcher.Switch.MinimumLevel
                }, logLevelSwitcher.Switch));
        }
    }

    public static class GraylogExtensions
    {
        public static LoggerConfiguration Graylog(this LoggerSinkConfiguration sinkConfiguration,
            GraylogSinkOptions options,
            LoggingLevelSwitch controllerSwitch)
        {
            return sinkConfiguration.Sink(new GraylogSink(options), levelSwitch: controllerSwitch);
        }
    }
}
