using System.Collections.Generic;
using Sitko.Core.App;

namespace Sitko.Core.Graylog
{
    public class GraylogLoggingOptions : BaseModuleConfig
    {
        public string Host { get; set; } = "locahost";
        public int Port { get; set; } = 22021;

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(Host))
                {
                    return (false, new[] {"Host can't be empty"});
                }

                if (Port == 0)
                {
                    return (false, new[] {"Port must be greater than 0"});
                }
            }

            return result;
        }
    }
}
