using System;
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
        
        public bool EnableRedisDataProtection { get; set; }
        public string RedisHost { get; set; }
        public int RedisPort { get; set; }
        public int RedisDb { get; set; } = -1;
        public TimeSpan DataProtectionLifeTime { get; set; } = TimeSpan.FromDays(90);
    }
}
