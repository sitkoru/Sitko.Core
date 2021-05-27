using FluentValidation;
using Sitko.Core.App;

namespace Sitko.Core.Graylog
{
    public class GraylogLoggingOptions : BaseModuleOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 22021;
    }

    public class GraylogLoggingOptionsValidator : AbstractValidator<GraylogLoggingOptions>
    {
        public GraylogLoggingOptionsValidator()
        {
            RuleFor(o => o.Host).NotEmpty().WithMessage("Host can't be empty");
            RuleFor(o => o.Port).GreaterThan(0).WithMessage("Port must be greater than 0");
        }
    }
}
