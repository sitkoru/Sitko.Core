using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Sitko.Core.Auth
{
    public class AuthOptions
    {
        public AuthOptions(string oidcServerUrl, string oidcClientId, string oidcClientSecret)
        {
            OidcServerUrl = oidcServerUrl;
            OidcClientId = oidcClientId;
            OidcClientSecret = oidcClientSecret;
        }

        public string OidcServerUrl { get; set; }
        public string OidcClientId { get; set; }
        public string OidcClientSecret { get; set; }
        public readonly List<string> OidcScopes = new List<string>();
        public bool EnableJwt { get; set; }
        
        public string JwtAudience { get; set; }
        public bool RequireHttps { get; set; }

        public readonly Dictionary<string, AuthorizationPolicy>
            Policies = new Dictionary<string, AuthorizationPolicy>();

        public string ForcePolicy { get; set; }
        public readonly List<string> IgnoreUrls = new List<string> {"/health", "/metrics"};
    }
}
