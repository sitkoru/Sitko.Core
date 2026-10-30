using FluentValidation;

namespace Sitko.Core.Auth.OIDC;

public abstract class OidcProviderAuthOptions : AuthOptions
{
    public string OidcServerUrl { get; set; } = "https://localhost";
    public bool RequireHttps { get; set; }
    public bool EnableHealthChecks { get; set; } = true;
}

public class OidcProviderAuthOptionsValidator<TOptions> : AuthOptionsValidator<TOptions>
    where TOptions : OidcProviderAuthOptions
{
    public OidcProviderAuthOptionsValidator() =>
        RuleFor(o => o.OidcServerUrl).NotEmpty().WithMessage("Oidc server url can't be empty");
}

