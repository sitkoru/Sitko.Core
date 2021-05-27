using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Sitko.Core.App;
using Sitko.Core.App.Web.Razor;

namespace Sitko.Core.Email
{
    public abstract class EmailModuleConfig : BaseModuleConfig, IViewToStringRendererServiceOptions
    {
        public string Host { get; set; } = "localhost";
        public string Scheme { get; set; } = "http";

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(Scheme))
                {
                    return (false, new[] {"Provide value for uri scheme to generate absolute urls"});
                }
            }

            return result;
        }
    }
}
