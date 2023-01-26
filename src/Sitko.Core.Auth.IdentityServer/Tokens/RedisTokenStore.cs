using System.Security.Claims;
using System.Text.Json;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Sitko.Core.Auth.IdentityServer.Tokens;

internal class RedisTokenStore : IUserTokenStore
{
    private readonly IDatabase redis;
    private readonly IDataProtector dataProtector;
    private readonly IOptions<OidcIdentityServerModuleOptions> options;

    public RedisTokenStore(IDataProtectionProvider dataProtectionProvider,
        IOptions<OidcIdentityServerModuleOptions> options)
    {
        this.options = options;
        redis = ConnectionMultiplexer.Connect($"{options.Value.RedisHost}:{options.Value.RedisPort}").GetDatabase(options.Value.RedisDb);
        dataProtector = dataProtectionProvider.CreateProtector(nameof(RedisTokenStore));
    }

    public Task<UserToken> GetTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null)
    {
        var userId = user.FindFirst(options.Value.UserIdClaimName)?.Value ??
                     throw new InvalidOperationException($"no {options.Value.UserIdClaimName} claim");

        var redisValue = redis.StringGet(userId);
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
        var userId = user.FindFirst(options.Value.UserIdClaimName)?.Value ??
                     throw new InvalidOperationException($"no {options.Value.UserIdClaimName} claim");
        var json = JsonSerializer.Serialize(token);
        var protectedJson = dataProtector.Protect(json);
        redis.StringSet(userId, protectedJson);

        return Task.CompletedTask;
    }

    public Task ClearTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null)
    {
        var userId = user.FindFirst(options.Value.UserIdClaimName)?.Value ??
                     throw new InvalidOperationException($"no {options.Value.UserIdClaimName} claim");

        redis.KeyDelete(userId);
        return Task.CompletedTask;
    }
}
