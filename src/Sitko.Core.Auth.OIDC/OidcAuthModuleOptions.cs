using Duende.AccessTokenManagement.OpenIdConnect;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Duende.IdentityModel;
using Sitko.Core.Auth.OIDC.Tokens;

namespace Sitko.Core.Auth.OIDC;

public class OidcAuthModuleOptions : OidcProviderAuthOptions
{
    public string? OidcClientId { get; set; }
    public string? OidcClientSecret { get; set; }
    public List<string> OidcScopes { get; } = new();
    public string ResponseType { get; set; } = OidcConstants.ResponseTypes.Code;
    public bool UsePkce { get; set; } = true;
    public bool SaveTokens { get; set; } = true;
    public bool GetClaimsFromUserInfoEndpoint { get; set; } = true;
    public override bool RequiresCookie => true;
    public override bool RequiresAuthentication => true;
    public override string SignInScheme => "Cookies";
    public override string ChallengeScheme => "oidc";
    public TokenStoreType TokenStoreType { get; set; } = TokenStoreType.None;
    public Action<OpenIdConnectOptions>? ConfigureOpenIdConnectOptions { get; set; }
    public Action<UserTokenManagementOptions>? ConfigureUserTokenManagement { get; set; }
}

public class OidcAuthOptionsValidator : OidcProviderAuthOptionsValidator<OidcAuthModuleOptions>
{
    public OidcAuthOptionsValidator()
    {
        RuleFor(o => o.OidcClientId).NotEmpty().WithMessage("Oidc client id can't be empty");
        RuleFor(o => o.OidcClientSecret).NotEmpty().WithMessage("Oidc client secret can't be empty");
    }

}
