using FluentValidation;

namespace Sitko.Core.Auth.Google;

public class GoogleAuthModuleOptions : AuthOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public List<string> Users { get; set; } = new();
    public override bool RequiresAuthentication => true;
    public override string SignInScheme => "Cookies";
    public override string ChallengeScheme => "Google";
    public override bool RequiresCookie => true;
}

public class GoogleAuthModuleOptionsValidator : AbstractValidator<GoogleAuthModuleOptions>
{
    public GoogleAuthModuleOptionsValidator()
    {
        RuleFor(o => o.ClientId).NotEmpty().WithMessage("ClientId can't be empty");
        RuleFor(o => o.ClientSecret).NotEmpty().WithMessage("ClientSecret can't be empty");
        RuleFor(o => o.SignInScheme).NotEmpty().WithMessage("SignInScheme can't be empty");
        RuleFor(o => o.ChallengeScheme).NotEmpty().WithMessage("ChallengeScheme can't be empty");
    }
}

