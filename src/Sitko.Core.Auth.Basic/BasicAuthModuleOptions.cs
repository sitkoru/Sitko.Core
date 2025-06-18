using FluentValidation;
using idunno.Authentication.Basic;

namespace Sitko.Core.Auth.Basic;

public class BasicAuthModuleOptions : AuthOptions
{
    public string Realm { get; set; } = "Basic Auth";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool AllowInsecureProtocol { get; set; }
    public override bool RequiresCookie => false;
    public override bool RequiresAuthentication => true;
    public override string SignInScheme => BasicAuthenticationDefaults.AuthenticationScheme;
    public override string ChallengeScheme => BasicAuthenticationDefaults.AuthenticationScheme;
}

public class BasicAuthModuleOptionsValidator : AbstractValidator<BasicAuthModuleOptions>
{
    public BasicAuthModuleOptionsValidator()
    {
        RuleFor(o => o.Realm).NotEmpty().WithMessage("Realm can't be empty");
        RuleFor(o => o.Username).NotEmpty().WithMessage("Username can't be empty");
        RuleFor(o => o.Password).NotEmpty().WithMessage("Password can't be empty");
    }
}
