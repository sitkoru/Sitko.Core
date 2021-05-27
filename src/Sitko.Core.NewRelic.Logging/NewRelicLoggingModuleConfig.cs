using System.Collections.Generic;
using FluentValidation;
using Sitko.Core.App;

namespace Sitko.Core.NewRelic.Logging
{
    public class NewRelicLoggingModuleOptions : BaseModuleOptions
    {
        public string LicenseKey { get; set; } = string.Empty;
        public bool EnableLogging { get; set; } = false;
        public string LogsUrl { get; set; } = "https://log-api.newrelic.com/log/v1";
    }

    public class NewRelicLoggingModuleOptionsValidator : AbstractValidator<NewRelicLoggingModuleOptions>
    {
        public NewRelicLoggingModuleOptionsValidator()
        {
            RuleFor(o => o.LicenseKey).NotEmpty().WithMessage("Provide License Key for NewRelic");
        }
    }
}
