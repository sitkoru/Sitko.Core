using System.Globalization;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Sitko.Core.Auth.IdentityServer.Tokens;

public class OidcEvents : OpenIdConnectEvents
{
    private readonly IUserTokenStore store;

    public OidcEvents(IUserTokenStore store) => this.store = store;

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        // TODO: WHY EMPTY?
        var expiration = !string.IsNullOrEmpty(context.TokenEndpointResponse!.ExpiresIn)
            ? TimeSpan.FromSeconds(double.Parse(context.TokenEndpointResponse!.ExpiresIn,
                CultureInfo.InvariantCulture))
            : TimeSpan.FromMinutes(30);
        var exp = DateTimeOffset.UtcNow.Add(expiration);

        await store.StoreTokenAsync(context.Principal!,
            new UserToken
            {
                AccessToken = context.TokenEndpointResponse.AccessToken,
                Expiration = exp,
                RefreshToken = context.TokenEndpointResponse.RefreshToken,
                Scope = context.TokenEndpointResponse.Scope
            });

        await base.TokenValidated(context);
    }
}
