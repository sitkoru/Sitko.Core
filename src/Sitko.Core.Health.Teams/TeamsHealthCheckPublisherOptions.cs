using System.Collections.Generic;
using Sitko.Core.App;

namespace Sitko.Core.Health.Teams
{
    public class TeamsHealthCheckPublisherOptions : BaseModuleConfig
    {
        public string WebHookUrl { get; set; } = string.Empty;
        public string UnHealthyColor { get; set; } = "#c74f4f";
        public string HealthyColor { get; set; } = "#91c337";
        public string DegradedColor { get; set; } = "#ffc107";

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(WebHookUrl))
                {
                    return (false, new[] {"Teams web hook url can't be empty"});
                }
            }

            return result;
        }
    }
}
