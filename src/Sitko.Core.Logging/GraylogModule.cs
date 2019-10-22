using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Sinks.Graylog;

namespace Sitko.Core.Logging
{
    public class GraylogModule : LoggingModule<GraylogLoggingOptions>
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            Config.ConfigureLogger = (loggerConfiguration, logLevelSwitcher) =>
            {
                loggerConfiguration.WriteTo.Async(to => to.Graylog(
                    new GraylogSinkOptions
                    {
                        HostnameOrAddress = Config.Host,
                        Port = Config.Port,
                        Facility = Config.Facility,
                        MinimumLogEventLevel = logLevelSwitcher.Switch.MinimumLevel
                    }, logLevelSwitcher.Switch));
            };
            base.ConfigureServices(services, configuration, environment);
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
