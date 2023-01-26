using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Sitko.Core.App;

namespace Sitko.Core.Auth;

public abstract class AuthOptions : BaseModuleOptions
{
    [JsonIgnore] public Dictionary<string, AuthorizationPolicy> Policies { get; } = new();
    public string? ForcePolicy { get; set; }
    public List<string> IgnoreUrls { get; set; } = new() { "/health", "/metrics" };
    public bool EnableRedisDataProtection { get; set; }
    public string? RedisHost { get; set; }
    public int RedisPort { get; set; } = 6379;
    public int RedisDb { get; set; } = -1;
    public int DataProtectionLifeTimeInMinutes { get; set; } = 90 * 24 * 60;
    public abstract bool RequiresCookie { get; }
    public abstract string SignInScheme { get; }
    public abstract string ChallengeScheme { get; }
    public int CookieExpireInMinutes { get; set; } = 30 * 24 * 60;
    public string UserIdClaimName { get; set; } = ClaimTypes.NameIdentifier;
    [JsonIgnore] public Action<CookieBuilder>? ConfigureCookie { get; set; }
}

public abstract class AuthOptionsValidator<TOptions> : AbstractValidator<TOptions> where TOptions : AuthOptions
{
    protected AuthOptionsValidator()
    {
        RuleFor(o => o.RedisHost).NotEmpty().When(o => o.EnableRedisDataProtection)
            .WithMessage("Redis host can't be empty when Redis Data protection enabled");
        RuleFor(o => o.RedisPort).GreaterThan(0).When(o => o.EnableRedisDataProtection)
            .WithMessage("Redis port can't be empty when Redis Data protection enabled");
    }
}

