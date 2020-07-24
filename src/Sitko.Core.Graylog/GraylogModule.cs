using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
        public override void ConfigureLogging(LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureLogging(loggerConfiguration, logLevelSwitcher, configuration, environment);
            loggerConfiguration.WriteTo.Async(to => to.Graylog(
                new GraylogSinkOptions
                {
                    HostnameOrAddress = Config.Host,
                    Port = Config.Port,
                    Facility = Application.Name,
                    MinimumLogEventLevel = logLevelSwitcher.Switch.MinimumLevel
                }, logLevelSwitcher.Switch));
        }

        public GraylogModule(GraylogLoggingOptions config, Application application) : base(config, application)
        {
        }

        public override void CheckConfig()
        {
            base.CheckConfig();
            if (string.IsNullOrEmpty(Config.Host))
            {
                throw new ArgumentException("Host can't be empty", nameof(Config.Host));
            }

            if (Config.Port == 0)
            {
                throw new ArgumentException("Port must be greater than 0", nameof(Config.Port));
            }
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
