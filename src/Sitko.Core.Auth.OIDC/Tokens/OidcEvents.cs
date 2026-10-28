using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.AccessTokenManagement;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Sitko.Core.Auth.OIDC.Tokens;

public class OidcEvents : OpenIdConnectEvents
{
    private static readonly object PendingTokenKey = new();
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

        context.HttpContext.Items[PendingTokenKey] = new UserToken
        {
            AccessToken = AccessToken.Parse(context.TokenEndpointResponse.AccessToken),
            ClientId = ClientId.Parse(context.Options.ClientId!),
            AccessTokenType = AccessTokenType.Parse(context.TokenEndpointResponse.TokenType ?? "Bearer"),
            Expiration = exp,
            RefreshToken = string.IsNullOrEmpty(context.TokenEndpointResponse.RefreshToken) ? null : RefreshToken.Parse(context.TokenEndpointResponse.RefreshToken),
            Scope = string.IsNullOrEmpty(context.TokenEndpointResponse.Scope) ? null : Scope.Parse(context.TokenEndpointResponse.Scope)
        };

        await base.TokenValidated(context);
    }
    public override async Task TicketReceived(TicketReceivedContext context)
    {
        await base.TicketReceived(context);
        if (context.Result is null && context.HttpContext.Items.Remove(PendingTokenKey, out var pending) && pending is UserToken token)
        {
            await store.StoreTokenAsync(context.Principal!, token, ct: context.HttpContext.RequestAborted);
        }
    }
}
