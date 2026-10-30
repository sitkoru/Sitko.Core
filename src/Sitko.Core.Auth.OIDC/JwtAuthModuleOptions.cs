using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Sitko.Core.Auth.OIDC;

public class JwtAuthModuleOptions : OidcProviderAuthOptions
{
    public string JwtAudience { get; set; } = string.Empty;
    public override bool RequiresCookie => false;
    public override bool RequiresAuthentication => true;
    public override string SignInScheme => JwtBearerDefaults.AuthenticationScheme;
    public override string ChallengeScheme => JwtBearerDefaults.AuthenticationScheme;
    public string[] ValidIssuers { get; set; } = [];
    public Action<JwtBearerOptions>? ConfigureJwtBearerOptions { get; set; }
}

public class JwtAuthOptionsValidator : OidcProviderAuthOptionsValidator<JwtAuthModuleOptions>
{
    public JwtAuthOptionsValidator() =>
        RuleFor(o => o.JwtAudience).NotEmpty().WithMessage("Oidc jwt audience can't be empty");
}
