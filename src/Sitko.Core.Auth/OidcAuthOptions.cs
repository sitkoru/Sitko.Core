using System.Collections.Generic;

namespace Sitko.Core.Auth
{
    public class OidcAuthOptions : AuthOptions
    {
        public OidcAuthOptions(string oidcServerUrl, string oidcClientId, string oidcClientSecret) : base(oidcServerUrl)
        {
            OidcClientId = oidcClientId;
            OidcClientSecret = oidcClientSecret;
        }

        public string OidcClientId { get; set; }
        public string OidcClientSecret { get; set; }
        public readonly List<string> OidcScopes = new List<string>();
        public readonly List<string> IgnoreUrls = new List<string> {"/health", "/metrics"};
    }
}
