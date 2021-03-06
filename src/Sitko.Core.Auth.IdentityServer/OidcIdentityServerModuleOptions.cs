using System.Collections.Generic;
using FluentValidation;
using IdentityModel;

namespace Sitko.Core.Auth.IdentityServer
{
    public class OidcIdentityServerModuleOptions : IdentityServerAuthOptions
    {
        public string? OidcClientId { get; set; }
        public string? OidcClientSecret { get; set; }
        public List<string> OidcScopes { get; } = new();
        public bool EnableRedisDataProtection { get; set; }
        public string? RedisHost { get; set; }
        public int RedisPort { get; set; }
        public int RedisDb { get; set; } = -1;
        public int DataProtectionLifeTimeInMinutes { get; set; } = 90 * 24 * 60;
        public string ResponseType { get; set; } = OidcConstants.ResponseTypes.Code;
        public bool UsePkce { get; set; } = true;
        public bool SaveTokens { get; set; } = true;
        public bool GetClaimsFromUserInfoEndpoint { get; set; } = true;
        public string SignInScheme { get; set; } = "Cookies";
        public string ChallengeScheme { get; set; } = "oidc";
        public int ExpireTimeSpanInMinutes { get; set; } = 30 * 24 * 60;
        public bool SlidingExpiration { get; set; } = true;
    }

    public class OidcAuthOptionsValidator : IdentityServerAuthOptionsValidator<OidcIdentityServerModuleOptions>
    {
        public OidcAuthOptionsValidator()
        {
            RuleFor(o => o.OidcClientId).NotEmpty().WithMessage("Oidc client id can't be empty");
            RuleFor(o => o.OidcClientSecret).NotEmpty().WithMessage("Oidc client secret can't be empty");
            RuleFor(o => o.RedisHost).NotEmpty().When(o => o.EnableRedisDataProtection)
                .WithMessage("Redis host can't be empty when Redis Data protection enabled");
            RuleFor(o => o.RedisPort).GreaterThan(0).When(o => o.EnableRedisDataProtection)
                .WithMessage("Redis port can't be empty when Redis Data protection enabled");
        }
    }
}
