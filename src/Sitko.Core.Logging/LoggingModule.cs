using System;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Sitko.Core.App;

namespace Sitko.Core.Logging
{
    public class LoggingModule<T> : BaseApplicationModule<T> where T : LoggingOptions
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            Console.OutputEncoding = Encoding.UTF8;
            var logLevelSwitcher = new LogLevelSwitcher();
            Config.Facility = Config.Facility ?? environment.ApplicationName;
            var loggerConfiguration =
                new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("App", Config.Facility);

            if (environment.IsDevelopment())
            {
                logLevelSwitcher.Switch.MinimumLevel = Config.DevLogLevel;
                logLevelSwitcher.MsMessagesSwitch.MinimumLevel = Config.DevLogLevel;
            }
            else
            {
                logLevelSwitcher.Switch.MinimumLevel = Config.ProdLogLevel;
                logLevelSwitcher.MsMessagesSwitch.MinimumLevel = LogEventLevel.Warning;
                loggerConfiguration.MinimumLevel.Override("Microsoft", logLevelSwitcher.MsMessagesSwitch);
            }

            if (Config.EnableConsoleLogging)
            {
                loggerConfiguration = loggerConfiguration
                    .WriteTo.Console(outputTemplate:"[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}",levelSwitch: logLevelSwitcher.Switch);
            }

            loggerConfiguration.MinimumLevel.ControlledBy(logLevelSwitcher.Switch);
            Config.ConfigureLogger?.Invoke(loggerConfiguration, logLevelSwitcher);
            Log.Logger = loggerConfiguration.CreateLogger();
            services.AddSingleton(logLevelSwitcher);
            services.AddSingleton(_ => (ILoggerFactory)new SerilogLoggerFactory());
        }
    }
}
