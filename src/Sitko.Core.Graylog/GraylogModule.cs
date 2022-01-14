using Serilog;
using Serilog.Sinks.Graylog;
using Sitko.Core.App;

namespace Sitko.Core.Graylog;

public class GraylogModule : BaseApplicationModule<GraylogModuleOptions>, ILoggingModule<GraylogModuleOptions>
{
    public override string OptionsKey => "Logging:Graylog";

    public void ConfigureLogging(IApplicationContext context, GraylogModuleOptions options,
        LoggerConfiguration loggerConfiguration) =>
        loggerConfiguration.WriteTo.Async(to => to.Graylog(
            new GraylogSinkOptions { HostnameOrAddress = options.Host, Port = options.Port, Facility = context.Name }));
}
