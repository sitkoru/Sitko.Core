using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.AccessTokenManagement;
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
        string? accessToken = null;
        var schemas = await authenticationSchemeProvider.GetAllSchemesAsync();

        if (userTokenManagementService is not null)
        {
            var oidcSchema = schemas.FirstOrDefault(scheme => scheme.Name == oidcOptions.Value.ChallengeScheme);
            if (oidcSchema is not null)
            {
                // we have token management service and available oidc schema, so try to get token this way first
                var schemaToken = await userTokenManagementService.GetAccessTokenAsync(
                    httpContext.User,
                    new UserTokenRequestParameters { ChallengeScheme = Scheme.Parse(oidcSchema.Name) }, cancellationToken);
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
}
