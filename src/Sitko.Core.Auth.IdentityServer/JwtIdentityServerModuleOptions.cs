using FluentValidation;

namespace Sitko.Core.Auth.IdentityServer
{
    public class JwtIdentityServerModuleOptions : IdentityServerAuthOptions
    {
        public string JwtAudience { get; set; } = string.Empty;
    }

    public class JwtAuthOptionsValidator : IdentityServerAuthOptionsValidator<JwtIdentityServerModuleOptions>
    {
        public JwtAuthOptionsValidator() => RuleFor(o => o.JwtAudience).NotEmpty().WithMessage("Oidc jwt audience can't be empty");
    }
}
