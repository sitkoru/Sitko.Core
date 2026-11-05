using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;

namespace Sitko.Core.App.OpenTelemetry
{
    public class OpenTelemetryModuleOptionsValidator : AbstractValidator<OpenTelemetryModuleOptions>
    {
        public OpenTelemetryModuleOptionsValidator() =>
            RuleFor(static options => options).Custom(static (options, context) =>
            {
                if (!options.Enabled || !options.EnableOtlpExport ||
                    !(options.EnableLogs || options.EnableMetrics || options.EnableTracing))
                {
                    return;
                }

                try
                {
                    _ = OtlpExportSettings.Resolve(options);
                }
                catch (OptionsValidationException exception)
                {
                    foreach (var failure in exception.Failures)
                    {
                        context.AddFailure(new ValidationFailure("OpenTelemetry", failure));
                    }
                }
            });
    }
}
