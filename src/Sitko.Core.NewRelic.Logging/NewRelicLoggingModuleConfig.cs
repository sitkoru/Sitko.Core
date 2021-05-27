using System.Collections.Generic;
using Sitko.Core.App;

namespace Sitko.Core.NewRelic.Logging
{
    public class NewRelicLoggingModuleConfig : BaseModuleConfig
    {
        public string LicenseKey { get; set; } = string.Empty;
        public bool EnableLogging { get; set; } = false;
        public string LogsUrl { get; set; } = "https://log-api.newrelic.com/log/v1";

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(LicenseKey))
                {
                    return (false, new[] {"Provide License Key for NewRelic"});
                }
            }

            return result;
        }
    }
}
