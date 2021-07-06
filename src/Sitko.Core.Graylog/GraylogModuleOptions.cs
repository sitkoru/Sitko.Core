using FluentValidation;
using Sitko.Core.App;

namespace Sitko.Core.Graylog
{
    public class GraylogModuleOptions : BaseModuleOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 22021;
    }

    public class GraylogModuleOptionsValidator : AbstractValidator<GraylogModuleOptions>
    {
        public GraylogModuleOptionsValidator()
        {
            RuleFor(o => o.Host).NotEmpty().WithMessage("Host can't be empty");
            RuleFor(o => o.Port).GreaterThan(0).WithMessage("Port must be greater than 0");
        }
    }
}
