using System.Text.Json;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.DPoP;
using Duende.AccessTokenManagement.OpenIdConnect;

namespace Sitko.Core.Auth.OIDC.Tokens
{
    // Keep the string-valued payload used before Duende 4, including in protected Redis records.
    internal sealed record StoredUserToken(
        string AccessToken,
        DateTimeOffset Expiration,
        string? RefreshToken,
        string? Scope,
        string? ClientId = null,
        string? AccessTokenType = null,
        string? DPoPJsonWebKey = null)
    {
        public static string Serialize(UserToken token) => JsonSerializer.Serialize(new StoredUserToken(
            token.AccessToken.ToString(), token.Expiration, token.RefreshToken?.ToString(), token.Scope?.ToString(),
            token.ClientId.ToString(), token.AccessTokenType?.ToString(), token.DPoPJsonWebKey?.ToString()));

        public static TokenResult<TokenForParameters> Deserialize(string json, string clientId)
        {
            var stored = JsonSerializer.Deserialize<StoredUserToken>(json);
            if (stored is null || string.IsNullOrEmpty(stored.AccessToken))
            {
                return TokenResult.Failure("bad token");
            }

            var token = new UserToken
            {
                AccessToken = Duende.AccessTokenManagement.AccessToken.Parse(stored.AccessToken),
                Expiration = stored.Expiration,
                RefreshToken = string.IsNullOrEmpty(stored.RefreshToken) ? null : Duende.AccessTokenManagement.RefreshToken.Parse(stored.RefreshToken),
                Scope = string.IsNullOrEmpty(stored.Scope) ? null : Duende.AccessTokenManagement.Scope.Parse(stored.Scope),
                ClientId = Duende.AccessTokenManagement.ClientId.Parse(stored.ClientId ?? clientId),
                AccessTokenType = Duende.AccessTokenManagement.AccessTokenType.Parse(stored.AccessTokenType ?? "Bearer"),
                DPoPJsonWebKey = string.IsNullOrEmpty(stored.DPoPJsonWebKey) ? null : DPoPProofKey.Parse(stored.DPoPJsonWebKey)
            };
            return new TokenForParameters(token, token.RefreshToken is { } refresh ? new UserRefreshToken(refresh, token.DPoPJsonWebKey) : null);
        }
    }
}
