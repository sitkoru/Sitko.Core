using Serilog;
using Serilog.Sinks.Graylog;
using Sitko.Core.App;

namespace Sitko.Core.Graylog
{
    public class GraylogModule : BaseApplicationModule<GraylogModuleOptions>
    {
        public override string OptionsKey => "Logging:Graylog";

        public override void ConfigureLogging(ApplicationContext context, GraylogModuleOptions options,
            LoggerConfiguration loggerConfiguration)
        {
            base.ConfigureLogging(context, options, loggerConfiguration);
            loggerConfiguration.WriteTo.Async(to => to.Graylog(
                new GraylogSinkOptions
                {
                    HostnameOrAddress = options.Host,
                    Port = options.Port,
                    Facility = context.Name
                }));
        }
    }
}
