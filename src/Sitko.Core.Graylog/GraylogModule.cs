using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Sinks.Graylog;
using Sitko.Core.App;
using Sitko.Core.App.Logging;

namespace Sitko.Core.Graylog
{
    public class GraylogModule : BaseApplicationModule<GraylogModuleOptions>
    {
        public override string OptionsKey => "Logging:Graylog";

        public override void ConfigureLogging(ApplicationContext context, GraylogModuleOptions options,
            LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher)
        {
            base.ConfigureLogging(context, options, loggerConfiguration, logLevelSwitcher);
            loggerConfiguration.WriteTo.Async(to => to.Graylog(
                new GraylogSinkOptions
                {
                    HostnameOrAddress = options.Host,
                    Port = options.Port,
                    Facility = context.Name,
                    MinimumLogEventLevel = logLevelSwitcher.Switch.MinimumLevel
                }, logLevelSwitcher.Switch));
        }
    }

    public static class GraylogExtensions
    {
        public static LoggerConfiguration Graylog(this LoggerSinkConfiguration sinkConfiguration,
            GraylogSinkOptions options,
            LoggingLevelSwitch controllerSwitch) =>
            sinkConfiguration.Sink(new GraylogSink(options), levelSwitch: controllerSwitch);
    }
}
