using FluentValidation;
using Sitko.Core.App;

namespace Sitko.Core.IdProvider.SonyFlake
{
    public class SonyFlakeModuleOptions : BaseModuleOptions
    {
        public string? Uri { get; set; }
    }

    public class SonyFlakeModuleConfigValidator : AbstractValidator<SonyFlakeModuleOptions>
    {
        public SonyFlakeModuleConfigValidator()
        {
            RuleFor(c => c.Uri).NotEmpty().WithMessage("Provide SonyFlake url");
        }
    }
}
