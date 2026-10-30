using System.Security.Claims;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.IdentityModel;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Sitko.Core.App;
using StackExchange.Redis;

namespace Sitko.Core.Auth.OIDC.Tokens;

internal class RedisTokenStore : IUserTokenStore
{
    private readonly IApplicationContext applicationContext;
    private readonly IDataProtector dataProtector;
    private readonly IOptions<OidcAuthModuleOptions> options;
    private readonly IDatabase redis;

    public RedisTokenStore(IDataProtectionProvider dataProtectionProvider, IApplicationContext applicationContext,
        IOptions<OidcAuthModuleOptions> options,
        IDatabase redis)
    {
        this.applicationContext = applicationContext;
        this.options = options;
        this.redis = redis;
        dataProtector = dataProtectionProvider.CreateProtector(nameof(RedisTokenStore));
    }

    public Task<TokenResult<TokenForParameters>> GetTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null, CancellationToken ct = default)
    {
        var userTokenKey = GetUserTokenKey(user);

        var redisValue = redis.StringGet(userTokenKey);
        if (redisValue.HasValue)
        {
            var protectedJson = redisValue.ToString();
            var json = dataProtector.Unprotect(protectedJson);
            return Task.FromResult(StoredUserToken.Deserialize(json, options.Value.OidcClientId!));
        }

        return Task.FromResult<TokenResult<TokenForParameters>>(TokenResult.Failure("not found"));
    }

    public Task StoreTokenAsync(ClaimsPrincipal user, UserToken token, UserTokenRequestParameters? parameters = null, CancellationToken ct = default)
    {
        var userTokenKey = GetUserTokenKey(user);
        var json = StoredUserToken.Serialize(token);
        var protectedJson = dataProtector.Protect(json);
        redis.StringSet(userTokenKey, protectedJson);

        return Task.CompletedTask;
    }

    public Task ClearTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null, CancellationToken ct = default)
    {
        var userTokenKey = GetUserTokenKey(user);

        redis.KeyDelete(userTokenKey);
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
