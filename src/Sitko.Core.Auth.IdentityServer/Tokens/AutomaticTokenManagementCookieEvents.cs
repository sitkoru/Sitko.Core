using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Sitko.Core.Auth.IdentityServer.Tokens;

public class AutomaticTokenManagementCookieEvents : CookieAuthenticationEvents
{
    private static readonly ConcurrentDictionary<string, bool> PendingRefreshTokenRequests = new();

    private readonly ISystemClock clock;
    private readonly ILogger logger;
    private readonly AutomaticTokenManagementOptions options;
    private readonly TokenEndpointService service;

    public AutomaticTokenManagementCookieEvents(
        TokenEndpointService service,
        IOptions<AutomaticTokenManagementOptions> options,
        ILogger<AutomaticTokenManagementCookieEvents> logger,
        ISystemClock clock)
    {
        this.service = service;
        this.options = options.Value;
        this.logger = logger;
        this.clock = clock;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var tokens = context.Properties.GetTokens();
        if (!tokens.Any())
        {
            logger.LogDebug(
                "No tokens found in cookie properties. SaveTokens must be enabled for automatic token refresh.");
            return;
        }

        var refreshToken = tokens.SingleOrDefault(t => t.Name == OpenIdConnectParameterNames.RefreshToken);
        if (refreshToken == null)
        {
            logger.LogWarning(
                "No refresh token found in cookie properties. A refresh token must be requested and SaveTokens must be enabled.");
            return;
        }

        var expiresAt = tokens.SingleOrDefault(t => t.Name == "expires_at");
        if (expiresAt == null)
        {
            logger.LogWarning("No expires_at value found in cookie properties.");
            return;
        }

        var dtExpires = DateTimeOffset.Parse(expiresAt.Value, CultureInfo.InvariantCulture);
        var dtRefresh = dtExpires.Subtract(options.RefreshBeforeExpiration);

        if (dtRefresh < clock.UtcNow)
        {
            var shouldRefresh = PendingRefreshTokenRequests.TryAdd(refreshToken.Value, true);
            if (shouldRefresh)
            {
                try
                {
                    var response = await service.RefreshTokenAsync(refreshToken.Value);

                    if (response.IsError)
                    {
                        logger.LogWarning("Error refreshing token: {Error}", response.Error);
                        return;
                    }

                    context.Properties.UpdateTokenValue("access_token", response.AccessToken);
                    context.Properties.UpdateTokenValue("refresh_token", response.RefreshToken);

                    var newExpiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(response.ExpiresIn);
                    context.Properties.UpdateTokenValue("expires_at",
                        newExpiresAt.ToString("o", CultureInfo.InvariantCulture));

                    await context.HttpContext.SignInAsync(context.Principal, context.Properties);
                }
                finally
                {
                    PendingRefreshTokenRequests.TryRemove(refreshToken.Value, out _);
                }
            }
        }
    }

    public override async Task SigningOut(CookieSigningOutContext context)
    {
        if (options.RevokeRefreshTokenOnSignout == false)
        {
            return;
        }

        var result = await context.HttpContext.AuthenticateAsync();

        if (!result.Succeeded)
        {
            logger.LogDebug("Can't find cookie for default scheme. Might have been deleted already.");
            return;
        }

        var tokens = result.Properties.GetTokens();
        if (!tokens.Any())
        {
            logger.LogDebug(
                "No tokens found in cookie properties. SaveTokens must be enabled for automatic token revocation.");
            return;
        }

        var refreshToken = tokens.SingleOrDefault(t => t.Name == OpenIdConnectParameterNames.RefreshToken);
        if (refreshToken == null)
        {
            logger.LogWarning(
                "No refresh token found in cookie properties. A refresh token must be requested and SaveTokens must be enabled.");
            return;
        }

        var response = await service.RevokeTokenAsync(refreshToken.Value);

        if (response.IsError)
        {
            logger.LogWarning("Error revoking token: {Error}", response.Error);
        }
    }
}
