namespace Sitko.Core.NewRelic.Logging
{
    public class NewRelicLoggingModuleConfig
    {
        public string LicenseKey { get; set; } = string.Empty;
        public bool EnableLogging { get; set; } = false;
        public string LogsUrl { get; set; } = "https://log-api.newrelic.com/log/v1";
    }
}
