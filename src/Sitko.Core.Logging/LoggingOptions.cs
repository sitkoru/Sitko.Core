using System;
using Serilog;
using Serilog.Events;

namespace Sitko.Core.Logging
{
    public class LoggingOptions
    {
        public LogEventLevel ProdLogLevel { get; set; } = LogEventLevel.Information;
        public LogEventLevel DevLogLevel { get; set; } = LogEventLevel.Debug;
        public bool EnableConsoleLogging { get; set; } = true;

        public LoggingOptions(string facility)
        {
            Facility = facility;
        }

        public string Facility { get; set; }
        
        public Action<LoggerConfiguration, LogLevelSwitcher> ConfigureLogger { get; set; }
    }
}
