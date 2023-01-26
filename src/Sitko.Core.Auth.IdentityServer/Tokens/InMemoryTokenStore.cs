using System.Security.Claims;
using System.Text.Json;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Auth.IdentityServer.Tokens;

internal class InMemoryTokenStore : IUserTokenStore
{
    private readonly IOptions<OidcIdentityServerModuleOptions> options;
    private readonly IMemoryCache memoryCache;

    public InMemoryTokenStore(IOptions<OidcIdentityServerModuleOptions> options, IMemoryCache memoryCache)
    {
        this.options = options;
        this.memoryCache = memoryCache;
    }

    public Task<UserToken> GetTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null)
    {
        var userId = user.FindFirst(options.Value.UserIdClaimName)?.Value ??
                     throw new InvalidOperationException($"no {options.Value.UserIdClaimName} claim");

        var value = memoryCache.Get<string>(userId);
        if (!string.IsNullOrEmpty(value))
        {
            var json = value;
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
        memoryCache.Set(userId, json);

        return Task.CompletedTask;
    }

    public Task ClearTokenAsync(ClaimsPrincipal user, UserTokenRequestParameters? parameters = null)
    {
        var userId = user.FindFirst(options.Value.UserIdClaimName)?.Value ??
                     throw new InvalidOperationException($"no {options.Value.UserIdClaimName} claim");

        memoryCache.Remove(userId);
        return Task.CompletedTask;
    }
}
