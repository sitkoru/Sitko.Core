using System.Security.Claims;
using System.Text.Json;
using Duende.AccessTokenManagement.OpenIdConnect;
using IdentityModel;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Sitko.Core.App;
using StackExchange.Redis;

namespace Sitko.Core.Auth.IdentityServer.Tokens;

internal class RedisTokenStore : IUserTokenStore
{
    private readonly IApplicationContext applicationContext;
    private readonly IDataProtector dataProtector;
    private readonly IOptions<OidcIdentityServerModuleOptions> options;
    private readonly Lazy<IDatabase> redis;

    public RedisTokenStore(IDataProtectionProvider dataProtectionProvider, IApplicationContext applicationContext,
        IOptions<OidcIdentityServerModuleOptions> options)
    {
        this.applicationContext = applicationContext;
        this.options = options;
        redis = new Lazy<IDatabase>(() =>
        {
            return ConnectionMultiplexer.Connect($"{options.Value.RedisHost}:{options.Value.RedisPort}",
                    configureOptions =>
                    {
                        if (!string.IsNullOrEmpty(options.Value.RedisPassword))
                        {
                            configureOptions.Password = options.Value.RedisPassword;
                        }
                    })
                .GetDatabase(options.Value.RedisDb);
        }, LazyThreadSafetyMode.ExecutionAndPublication);
        dataProtector = dataProtectionProvider.CreateProtector(nameof(RedisTokenStore));
    }

    public Task<UserToken> GetTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null)
    {
        var userTokenKey = GetUserTokenKey(user);

        var redisValue = redis.Value.StringGet(userTokenKey);
        if (redisValue.HasValue)
        {
            var protectedJson = redisValue.ToString();
            var json = dataProtector.Unprotect(protectedJson);
            var token = JsonSerializer.Deserialize<UserToken>(json);
            if (token is null)
            {
                return Task.FromResult(new UserToken { Error = "bad token" });
            }

            return Task.FromResult(token);
        }

        return Task.FromResult(new UserToken { Error = "not found" });
    }

    public Task StoreTokenAsync(ClaimsPrincipal user, UserToken token, UserTokenRequestParameters? parameters = null)
    {
        var userTokenKey = GetUserTokenKey(user);
        var json = JsonSerializer.Serialize(token);
        var protectedJson = dataProtector.Protect(json);
        redis.Value.StringSet(userTokenKey, protectedJson);

        return Task.CompletedTask;
    }

    public Task ClearTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null)
    {
        var userTokenKey = GetUserTokenKey(user);

        redis.Value.KeyDelete(userTokenKey);
        return Task.CompletedTask;
    }

    private string GetUserTokenKey(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(options.Value.UserIdClaimName)?.Value ??
                     throw new InvalidOperationException($"no {options.Value.UserIdClaimName} claim");
        // token will be unique for user, application and environment
        return $"DOT_{applicationContext.Name}_{applicationContext.Environment}_{userId}".ToSha256();
    }
}
