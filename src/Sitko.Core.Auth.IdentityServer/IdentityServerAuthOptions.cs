using FluentValidation;

namespace Sitko.Core.Auth.IdentityServer;

public abstract class IdentityServerAuthOptions : AuthOptions
{
    public string OidcServerUrl { get; set; } = "https://localhost";
    public bool RequireHttps { get; set; }
    public bool EnableHealthChecks { get; set; } = true;
}

public class IdentityServerAuthOptionsValidator<TOptions> : AuthOptionsValidator<TOptions>
    where TOptions : IdentityServerAuthOptions
{
    public IdentityServerAuthOptionsValidator() =>
        RuleFor(o => o.OidcServerUrl).NotEmpty().WithMessage("Oidc server url can't be empty");
}

