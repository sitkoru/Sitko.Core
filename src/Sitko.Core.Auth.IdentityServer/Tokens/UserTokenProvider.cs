using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Auth.IdentityServer.Tokens;

class UserTokenProvider : IUserTokenProvider
{
    private readonly IUserTokenManagementService? userTokenManagementService;
    private readonly IAuthenticationSchemeProvider authenticationSchemeProvider;
    private readonly IOptions<OidcIdentityServerModuleOptions> oidcOptions;

    public UserTokenProvider(IAuthenticationSchemeProvider authenticationSchemeProvider,
        IOptions<OidcIdentityServerModuleOptions> oidcOptions,
        IUserTokenManagementService? userTokenManagementService = null)
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
                    new UserTokenRequestParameters { ChallengeScheme = oidcSchema.Name }, cancellationToken);
                if (!string.IsNullOrEmpty(schemaToken.AccessToken) && !schemaToken.IsError)
                {
                    // we have token, no need to check other schemas
                    return schemaToken.AccessToken;
                }
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
