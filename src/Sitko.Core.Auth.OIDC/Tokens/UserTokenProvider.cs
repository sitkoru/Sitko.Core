using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.AccessTokenManagement;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Auth.OIDC.Tokens;

internal class UserTokenProvider : IUserTokenProvider
{
    private readonly IUserTokenManager? userTokenManagementService;
    private readonly IAuthenticationSchemeProvider authenticationSchemeProvider;
    private readonly IOptions<OidcAuthModuleOptions> oidcOptions;

    public UserTokenProvider(IAuthenticationSchemeProvider authenticationSchemeProvider,
        IOptions<OidcAuthModuleOptions> oidcOptions,
        IUserTokenManager? userTokenManagementService = null)
    {
        this.userTokenManagementService = userTokenManagementService;
        this.authenticationSchemeProvider = authenticationSchemeProvider;
        this.oidcOptions = oidcOptions;
    }

    public async Task<string?> GetUserTokenAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var user = httpContext.User;
        if (oidcOptions.Value.EnableSessionRecovery)
        {
            var ticket = await httpContext.AuthenticateAsync(oidcOptions.Value.SignInScheme);
            if (!IsActiveScheme(httpContext, ticket, oidcOptions.Value.SignInScheme))
            {
                foreach (var scheme in await authenticationSchemeProvider.GetAllSchemesAsync())
                {
                    if (scheme.Name == oidcOptions.Value.SignInScheme)
                    {
                        continue;
                    }
                    var other = await httpContext.AuthenticateAsync(scheme.Name);
                    if (IsActiveScheme(httpContext, other, scheme.Name))
                    {
                        var token = other.Properties?.GetTokenValue("access_token");
                        if (!string.IsNullOrEmpty(token))
                        {
                            return token;
                        }
                    }
                }
                return null;
            }
            user = ticket.Principal!;
            if (userTokenManagementService is null)
            {
                var token = ticket.Properties?.GetTokenValue("access_token");
                var expiresAt = ticket.Properties?.GetTokenValue("expires_at");
                if (string.IsNullOrEmpty(token) ||
                    (DateTimeOffset.TryParse(expiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                        out var expiration) && expiration <= DateTimeOffset.UtcNow))
                {
                    await UserSessionRecoveryContext.RequestRecoveryAsync();
                    throw new InvalidUserSessionException();
                }
                return token;
            }
        }

        string? accessToken = null;
        var schemas = await authenticationSchemeProvider.GetAllSchemesAsync();

        if (userTokenManagementService is not null)
        {
            var oidcSchema = schemas.FirstOrDefault(scheme => scheme.Name == oidcOptions.Value.ChallengeScheme);
            if (oidcSchema is not null)
            {
                // we have token management service and available oidc schema, so try to get token this way first
                var schemaToken = await userTokenManagementService.GetAccessTokenAsync(
                    user,
                    new UserTokenRequestParameters { ChallengeScheme = Scheme.Parse(oidcSchema.Name) }, cancellationToken);
                if (!schemaToken.Succeeded && oidcOptions.Value.EnableSessionRecovery &&
                    schemaToken.FailedResult?.Error is "invalid_grant" or "not found")
                {
                    await UserSessionRecoveryContext.RequestRecoveryAsync();
                    throw new InvalidUserSessionException();
                }
                // A failed refresh must not fall back to the stale token saved in the cookie.
                return schemaToken.Succeeded ? schemaToken.Token.AccessToken.ToString() : null;
            }
        }

        // no oidc token, so check other schemas
        foreach (var schema in await authenticationSchemeProvider.GetAllSchemesAsync())
        {
            var schemaToken = await httpContext.GetTokenAsync(schema.Name, "access_token");
            if (!string.IsNullOrEmpty(schemaToken))
            {
                accessToken = schemaToken;
                break;
            }
        }

        return accessToken;
    }

    private static bool IsActiveScheme(HttpContext context, AuthenticateResult result, string scheme)
    {
        if (!result.Succeeded || result.Principal is null)
        {
            return false;
        }
        var active = context.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult;
        if (active?.Succeeded == true && active.Ticket is not null)
        {
            return active.Ticket.AuthenticationScheme.Split(';').Contains(scheme, StringComparer.Ordinal);
        }
        return result.Principal.Identities.Any(identity =>
            context.User.Identities.Any(current => ReferenceEquals(identity, current)));
    }
}
