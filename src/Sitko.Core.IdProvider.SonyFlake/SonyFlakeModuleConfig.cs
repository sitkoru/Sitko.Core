using System.Collections.Generic;
using Sitko.Core.App;

namespace Sitko.Core.IdProvider.SonyFlake
{
    public class SonyFlakeModuleConfig : BaseModuleConfig
    {
        public string Uri { get; set; } = "http://id.localhost";

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(Uri))
                {
                    return (false, new[] {"Provide sonyflake url"});
                }

                return result;
            }

            return result;
        }
    }
}
