using System.Collections.Generic;

namespace Sitko.Core.Search.ElasticSearch
{
    public class ElasticSearchModuleConfig : SearchModuleConfig
    {
        public string Prefix { get; set; } = string.Empty;
        public string Url { get; set; } = "http://localhost:9200";
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableClientLogging { get; set; } = false;

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(Url))
                {
                    return (false, new[] {"Elastic url is empty"});
                }
            }

            return result;
        }
    }
}
