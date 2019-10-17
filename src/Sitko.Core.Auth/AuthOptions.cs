using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Sitko.Core.Auth
{
    public abstract class AuthOptions
    {
        protected AuthOptions(string oidcServerUrl)
        {
            OidcServerUrl = oidcServerUrl;
        }

        public string OidcServerUrl { get; set; }
        public bool RequireHttps { get; set; }
        
        public readonly Dictionary<string, AuthorizationPolicy>
            Policies = new Dictionary<string, AuthorizationPolicy>();
        public string ForcePolicy { get; set; }
    }
}
