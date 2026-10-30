using System.Security.Claims;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Auth.OIDC.Tokens;

internal class InMemoryTokenStore : IUserTokenStore
{
    private readonly IOptions<OidcAuthModuleOptions> options;
    private readonly IMemoryCache memoryCache;

    public InMemoryTokenStore(IOptions<OidcAuthModuleOptions> options, IMemoryCache memoryCache)
    {
        this.options = options;
        this.memoryCache = memoryCache;
    }

    public Task<TokenResult<TokenForParameters>> GetTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null, CancellationToken ct = default)
    {
        var userId = user.FindFirst(options.Value.UserIdClaimName)?.Value ??
                     throw new InvalidOperationException($"no {options.Value.UserIdClaimName} claim");

        var value = memoryCache.Get<string>(userId);
        if (!string.IsNullOrEmpty(value))
        {
            var json = value;
            return Task.FromResult(StoredUserToken.Deserialize(json, options.Value.OidcClientId!));
        }

        return Task.FromResult<TokenResult<TokenForParameters>>(TokenResult.Failure("not found"));
    }

    public Task StoreTokenAsync(ClaimsPrincipal user, UserToken token, UserTokenRequestParameters? parameters = null, CancellationToken ct = default)
    {
        var userId = user.FindFirst(options.Value.UserIdClaimName)?.Value ??
                     throw new InvalidOperationException($"no {options.Value.UserIdClaimName} claim");
        var json = StoredUserToken.Serialize(token);
        memoryCache.Set(userId, json);

        return Task.CompletedTask;
    }

    public Task ClearTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null, CancellationToken ct = default)
    {
        var userId = user.FindFirst(options.Value.UserIdClaimName)?.Value ??
                     throw new InvalidOperationException($"no {options.Value.UserIdClaimName} claim");

        memoryCache.Remove(userId);
        return Task.CompletedTask;
    }
}
