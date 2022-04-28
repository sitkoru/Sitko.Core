using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Auth.IdentityServer.Tokens;

public class TokenEndpointService
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly AutomaticTokenManagementOptions managementOptions;
    private readonly IOptionsSnapshot<OpenIdConnectOptions> oidcOptions;
    private readonly IAuthenticationSchemeProvider schemeProvider;

    public TokenEndpointService(
        IOptions<AutomaticTokenManagementOptions> managementOptions,
        IOptionsSnapshot<OpenIdConnectOptions> oidcOptions,
        IAuthenticationSchemeProvider schemeProvider,
        IHttpClientFactory httpClientFactory)
    {
        this.managementOptions = managementOptions.Value;
        this.oidcOptions = oidcOptions;
        this.schemeProvider = schemeProvider;
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        var oidcOptions = await GetOidcOptionsAsync();
        var configuration =
            await oidcOptions.ConfigurationManager.GetConfigurationAsync(default);

        var tokenClient = httpClientFactory.CreateClient("tokenClient");

        return await tokenClient.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = configuration.TokenEndpoint,
            ClientId = oidcOptions.ClientId,
            ClientSecret = oidcOptions.ClientSecret,
            RefreshToken = refreshToken
        });
    }

    public async Task<TokenRevocationResponse> RevokeTokenAsync(string refreshToken)
    {
        var oidcOptions = await GetOidcOptionsAsync();
        var configuration =
            await oidcOptions.ConfigurationManager.GetConfigurationAsync(default);

        var tokenClient = httpClientFactory.CreateClient("tokenClient");

        return await tokenClient.RevokeTokenAsync(new TokenRevocationRequest
        {
            Address = configuration.AdditionalData[OidcConstants.Discovery.RevocationEndpoint].ToString(),
            ClientId = oidcOptions.ClientId,
            ClientSecret = oidcOptions.ClientSecret,
            Token = refreshToken,
            TokenTypeHint = OidcConstants.TokenTypes.RefreshToken
        });
    }

    private async Task<OpenIdConnectOptions> GetOidcOptionsAsync()
    {
        if (string.IsNullOrEmpty(managementOptions.Scheme))
        {
            var scheme = await schemeProvider.GetDefaultChallengeSchemeAsync();
            return oidcOptions.Get(scheme.Name);
        }

        return oidcOptions.Get(managementOptions.Scheme);
    }
}