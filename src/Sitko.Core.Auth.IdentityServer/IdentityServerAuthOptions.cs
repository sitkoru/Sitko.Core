using System.Collections.Generic;

namespace Sitko.Core.Auth.IdentityServer
{
    public abstract class IdentityServerAuthOptions : AuthOptions
    {
        public string OidcServerUrl { get; set; } = "https://localhost";
        public bool RequireHttps { get; set; }

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(OidcServerUrl))
                {
                    return (false, new[] {"Oidc server url can't be empty"});
                }
            }

            return result;
        }
    }
}
