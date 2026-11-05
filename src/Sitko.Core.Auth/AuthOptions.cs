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
    public int DataProtectionLifeTimeInMinutes { get; set; } = 90 * 24 * 60;
    public abstract bool RequiresCookie { get; }
    public abstract bool RequiresAuthentication { get; }
    public abstract string SignInScheme { get; }
    public abstract string ChallengeScheme { get; }
    public int CookieExpireInMinutes { get; set; } = 30 * 24 * 60;
    public string UserIdClaimName { get; set; } = ClaimTypes.NameIdentifier;
    [JsonIgnore] public Action<CookieBuilder>? ConfigureCookie { get; set; }

    public AuthOptions AddPolicy(string name, Func<AuthorizationPolicyBuilder, AuthorizationPolicyBuilder> policyBuilder,
        bool forcePolicy = false)
    {
        var builder = new AuthorizationPolicyBuilder();
        var policy = policyBuilder(builder).Build();
        return AddPolicy(name, policy, forcePolicy);
    }

    public AuthOptions AddPolicy(string name, AuthorizationPolicy policy, bool forcePolicy = false)
    {
        Policies.Add(name, policy);
        if (forcePolicy)
        {
            ForcePolicy = name;
        }

        return this;
    }
}

public abstract class AuthOptionsValidator<TOptions> : AbstractValidator<TOptions> where TOptions : AuthOptions;
