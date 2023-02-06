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

    public UserTokenProvider(IUserTokenManagementService? userTokenManagementService,
        IAuthenticationSchemeProvider authenticationSchemeProvider,
        IOptions<OidcIdentityServerModuleOptions> oidcOptions)
    {
        this.userTokenManagementService = userTokenManagementService;
        this.authenticationSchemeProvider = authenticationSchemeProvider;
        this.oidcOptions = oidcOptions;
    }

    public async Task<string?> GetUserTokenAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        string? accessToken = null;
        foreach (var schema in await authenticationSchemeProvider.GetAllSchemesAsync())
        {
            if (userTokenManagementService != null && schema.Name == oidcOptions.Value.ChallengeScheme)
            {
                var schemaToken = await userTokenManagementService.GetAccessTokenAsync(
                    httpContext.User,
                    new UserTokenRequestParameters { ChallengeScheme = schema.Name }, cancellationToken);
                if (!string.IsNullOrEmpty(schemaToken.AccessToken) && !schemaToken.IsError)
                {
                    accessToken = schemaToken.AccessToken;
                    break;
                }
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                var schemaToken = await httpContext.GetTokenAsync(schema.Name, "access_token");
                if (!string.IsNullOrEmpty(schemaToken))
                {
                    accessToken = schemaToken;
                    break;
                }
            }
        }

        return accessToken;
    }
}
