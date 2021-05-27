using System.Collections.Generic;

namespace Sitko.Core.Auth.IdentityServer
{
    public class JwtAuthOptions : IdentityServerAuthOptions
    {
        public string JwtAudience { get; set; } = string.Empty;

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(JwtAudience))
                {
                    return (false, new[] {"Oidc jwt audience can't be empty"});
                }
            }

            return result;
        }
    }
}
