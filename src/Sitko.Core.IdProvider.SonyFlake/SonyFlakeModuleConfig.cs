using FluentValidation;

namespace Sitko.Core.IdProvider.SonyFlake;

public class SonyFlakeIdProviderModuleOptions : BaseIdProviderModuleOptions<SonyFlakeIdProvider>
{
    public string Uri { get; set; } = "http://localhost:9200";
}

public class SonyFlakeIdProviderModuleOptionsValidator : AbstractValidator<SonyFlakeIdProviderModuleOptions>
{
    public SonyFlakeIdProviderModuleOptionsValidator() =>
        RuleFor(c => c.Uri).NotEmpty().WithMessage("Provide SonyFlake url");
}

