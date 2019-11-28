using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Sinks.Graylog;

namespace Sitko.Core.Logging
{
    public class GraylogModule : LoggingModule<GraylogLoggingOptions>
    {
        protected override void ConfigureLogger(LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher)
        {
            base.ConfigureLogger(loggerConfiguration, logLevelSwitcher);
            loggerConfiguration.WriteTo.Async(to => to.Graylog(
                new GraylogSinkOptions
                {
                    HostnameOrAddress = Config.Host,
                    Port = Config.Port,
                    Facility = Config.Facility,
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
