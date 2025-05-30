using Duende.AccessTokenManagement.OpenIdConnect;
using FluentValidation;
using IdentityModel;
using Sitko.Core.Auth.IdentityServer.Tokens;

namespace Sitko.Core.Auth.IdentityServer;

public class OidcIdentityServerModuleOptions : IdentityServerAuthOptions
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
    public Action<UserTokenManagementOptions>? ConfigureUserTokenManagement { get; set; }
}

public class OidcAuthOptionsValidator : IdentityServerAuthOptionsValidator<OidcIdentityServerModuleOptions>
{
    public OidcAuthOptionsValidator()
    {
        RuleFor(o => o.OidcClientId).NotEmpty().WithMessage("Oidc client id can't be empty");
        RuleFor(o => o.OidcClientSecret).NotEmpty().WithMessage("Oidc client secret can't be empty");
        RuleFor(o => o.RedisHost).NotEmpty().When(o => o.TokenStoreType == TokenStoreType.Redis)
            .WithMessage("Redis host can't be empty when TokenStore configure to redis");
        RuleFor(o => o.RedisPort).GreaterThan(0).When(o => o.TokenStoreType == TokenStoreType.Redis)
            .WithMessage("Redis port can't be empty when TokenStore configure to redis");
    }
}

