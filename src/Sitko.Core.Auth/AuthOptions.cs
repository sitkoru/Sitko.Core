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
    public int RedisDb { get; set; }
    public string? RedisPassword { get; set; }
    public string? RedisUser { get; set; }
    public string[] RedisSentinelEndpoints { get; set; } = [];
    public string? RedisServiceName { get; set; }
    public string? RedisSentinelUser { get; set; }
    public string? RedisSentinelPassword { get; set; }
    internal bool UseRedisSentinel => RedisServiceName is not null || RedisSentinelEndpoints is { Length: > 0 };
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

public abstract class AuthOptionsValidator<TOptions> : AbstractValidator<TOptions> where TOptions : AuthOptions
{
    protected AuthOptionsValidator()
    {
        _ = When(RequiresRedis, () =>
        {
            _ = RuleFor(static o => o.RedisHost).NotEmpty().When(static o => !o.UseRedisSentinel)
                .WithMessage("Redis host can't be empty for a direct connection");
            _ = RuleFor(static o => o.RedisPort).InclusiveBetween(1, 65535).When(static o => !o.UseRedisSentinel);
            _ = RuleFor(static o => o.RedisServiceName).NotEmpty().When(static o => o.UseRedisSentinel)
                .WithMessage("Redis service name is required for Sentinel");
            _ = RuleFor(static o => o.RedisSentinelEndpoints).NotEmpty().When(static o => o.UseRedisSentinel)
                .WithMessage("At least one Redis Sentinel endpoint is required");
            _ = RuleForEach(static o => o.RedisSentinelEndpoints).Must(IsValidSentinelEndpoint)
                .WithMessage("Redis Sentinel endpoint must be a host:port address with a valid port");
        });
        _ = RuleFor(static o => o.RedisDb).GreaterThanOrEqualTo(0).When(static o => o.EnableRedisDataProtection)
            .WithMessage("Can't use -1 database for data protection");
    }

    private static bool IsValidSentinelEndpoint(string endpoint) =>
        Uri.TryCreate($"redis://{endpoint}", UriKind.Absolute, out var uri) &&
        uri.Port > 0 && uri.Host.Length > 0 && uri.UserInfo.Length == 0 &&
        uri.AbsolutePath == "/" && uri.Query.Length == 0 && uri.Fragment.Length == 0;

    protected virtual bool RequiresRedis(TOptions options) => options.EnableRedisDataProtection;
}
