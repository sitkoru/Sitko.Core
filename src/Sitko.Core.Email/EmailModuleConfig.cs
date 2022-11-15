using FluentValidation;
using Sitko.Core.App;

namespace Sitko.Core.Email;

public abstract class EmailModuleOptions : BaseModuleOptions
{
    public string Host { get; set; } = "localhost";
    public string Scheme { get; set; } = "http";
}

public abstract class EmailModuleOptionsValidator<TOptions> : AbstractValidator<TOptions>
    where TOptions : EmailModuleOptions
{
    public EmailModuleOptionsValidator() => RuleFor(o => o.Scheme).NotEmpty()
        .WithMessage("Provide value for uri scheme to generate absolute urls");
}

