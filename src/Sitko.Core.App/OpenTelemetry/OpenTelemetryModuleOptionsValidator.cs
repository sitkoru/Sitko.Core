using FluentValidation;

namespace Sitko.Core.App.OpenTelemetry;

public class OpenTelemetryModuleOptionsValidator : AbstractValidator<OpenTelemetryModuleOptions>
{
    public OpenTelemetryModuleOptionsValidator() =>
        RuleFor(o => o.Endpoint).NotNull().When(options => options.EnableOtlpExport);
}
