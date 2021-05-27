using System.Collections.Generic;

namespace Sitko.Core.Email
{
    public abstract class FluentEmailModuleConfig : EmailModuleConfig
    {
        public string From { get; set; } = "admin@localhost";

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(From))
                {
                    return (false, new[] {"Provide value for from address"});
                }
            }

            return result;
        }
    }
}
