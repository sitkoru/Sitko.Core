using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Sitko.Core.App;
using Sitko.Core.Redis;

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
    public int DataProtectionLifeTimeInMinutes { get; set; } = 90 * 24 * 60;
    public abstract bool RequiresCookie { get; }
    public abstract bool RequiresAuthentication { get; }
    public abstract string SignInScheme { get; }
    public abstract string ChallengeScheme { get; }
    public int CookieExpireInMinutes { get; set; } = 30 * 24 * 60;
    public string UserIdClaimName { get; set; } = ClaimTypes.NameIdentifier;
    [JsonIgnore] public Action<CookieBuilder>? ConfigureCookie { get; set; }

    internal RedisModuleOptions GetRedisOptions() => new()
    {
        Host = RedisHost,
        Port = RedisPort,
        Db = RedisDb,
        Password = RedisPassword,
        User = RedisUser,
        SentinelEndpoints = RedisSentinelEndpoints,
        ServiceName = RedisServiceName,
        SentinelUser = RedisSentinelUser,
        SentinelPassword = RedisSentinelPassword
    };

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
        _ = RuleFor(static o => o).Custom(static (options, context) =>
        {
            foreach (var failure in new RedisModuleOptionsValidator().Validate(options.GetRedisOptions()).Errors)
            {
                context.AddFailure($"Redis{failure.PropertyName}", failure.ErrorMessage);
            }
        }).When(RequiresRedis);
        _ = RuleFor(static o => o.RedisDb).GreaterThanOrEqualTo(0).When(static o => o.EnableRedisDataProtection)
            .WithMessage("Can't use -1 database for data protection");
    }

    protected virtual bool RequiresRedis(TOptions options) => options.EnableRedisDataProtection;
}
