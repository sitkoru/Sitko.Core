using FluentValidation;

namespace Sitko.Core.Email
{
    public abstract class FluentEmailModuleOptions : EmailModuleOptions
    {
        public string From { get; set; } = "admin@localhost";
    }

    public abstract class FluentEmailModuleOptionsValidator<TOptions> : EmailModuleOptionsValidator<TOptions>
        where TOptions : FluentEmailModuleOptions
    {
        public FluentEmailModuleOptionsValidator() => RuleFor(o => o.From).NotEmpty().WithMessage("Provide value for from address");
    }
}
