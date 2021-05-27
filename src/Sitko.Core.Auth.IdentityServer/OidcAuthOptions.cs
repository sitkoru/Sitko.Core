using System;
using System.Collections.Generic;
using IdentityModel;

namespace Sitko.Core.Auth.IdentityServer
{
    public class OidcAuthOptions : IdentityServerAuthOptions
    {
        public string? OidcClientId { get; set; }
        public string? OidcClientSecret { get; set; }
        public readonly List<string> OidcScopes = new List<string>();

        public bool EnableRedisDataProtection { get; set; }
        public string? RedisHost { get; set; }
        public int RedisPort { get; set; }
        public int RedisDb { get; set; } = -1;
        public TimeSpan DataProtectionLifeTime { get; set; } = TimeSpan.FromDays(90);
        public string ResponseType { get; set; } = OidcConstants.ResponseTypes.Code;
        public bool UsePkce { get; set; } = true;
        public bool SaveTokens { get; set; } = true;
        public bool GetClaimsFromUserInfoEndpoint { get; set; } = true;
        public string SignInScheme { get; set; } = "Cookies";
        public string ChallengeScheme { get; set; } = "oidc";
        public TimeSpan ExpireTimeSpan { get; set; } = TimeSpan.FromDays(30);
        public bool SlidingExpiration { get; set; } = true;

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(OidcClientId))
                {
                    return (false, new[] {"Oidc client id can't be empty"});
                }

                if (string.IsNullOrEmpty(OidcClientSecret))
                {
                    return (false, new[] {"Oidc client secret can't be empty"});
                }

                if (EnableRedisDataProtection)
                {
                    if (string.IsNullOrEmpty(RedisHost))
                    {
                        return (false, new[] {"Redis host can't be empty when Redis Data protection enabled"});
                    }

                    if (RedisPort == 0)
                    {
                        return (false, new[] {"Redis port can't be empty when Redis Data protection enabled"});
                    }
                }
            }

            return result;
        }
    }
}
