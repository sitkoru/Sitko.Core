using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NewRelic.LogEnrichers.Serilog;
using Serilog;
using Sitko.Core.App;
using Sitko.Core.App.Logging;

namespace Sitko.Core.NewRelic.Logging
{
    public class NewRelicLoggingModule : BaseApplicationModule<NewRelicLoggingModuleConfig>
    {
        public NewRelicLoggingModule(NewRelicLoggingModuleConfig config, Application application) : base(config,
            application)
        {
        }

        public override void ConfigureLogging(LoggerConfiguration loggerConfiguration,
            LogLevelSwitcher logLevelSwitcher,
            IConfiguration configuration, IHostEnvironment environment)
        {
            base.ConfigureLogging(loggerConfiguration, logLevelSwitcher, configuration, environment);
            if (Config.EnableLogging)
            {
                loggerConfiguration
                    .Enrich.WithNewRelicLogsInContext()
                    .WriteTo.NewRelicLogs(Config.LogsUrl,
                        environment.ApplicationName,
                        Config.LicenseKey);
            }
        }

        public override void CheckConfig()
        {
            base.CheckConfig();
            if (string.IsNullOrEmpty(Config.LicenseKey))
            {
                throw new ArgumentException("Provide License Key for NewRelic");
            }
        }
    }
}
