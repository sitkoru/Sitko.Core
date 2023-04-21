using FluentValidation;
using Sentry.AspNetCore;
using Sitko.Core.App;

namespace Sitko.Core.Sentry;

public class SentryModuleOptions : BaseModuleOptions
{
    public string Dsn { get; set; } = "";
    public bool EnableDebug { get; set; }
    public double TracesSampleRate { get; set; } = 1.0;
    public Action<IApplicationContext, ISentryBuilder, SentryModuleOptions>? ConfigureSentry { get; set; }
}

public class SentryModuleOptionsValidator : AbstractValidator<SentryModuleOptions>
{
    public SentryModuleOptionsValidator()
    {
        RuleFor(options => options.Dsn).NotEmpty().WithMessage("Provide Sentry DSN");
        RuleFor(options => options.TracesSampleRate).GreaterThan(0).WithMessage("Traces Sample Rate should be greater than zero");
    }
}
