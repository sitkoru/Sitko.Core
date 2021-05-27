using FluentValidation;
using Sitko.Core.App;

namespace Sitko.Core.Health.Teams
{
    public class TeamsHealthCheckPublisherOptions : BaseModuleOptions
    {
        public string WebHookUrl { get; set; } = string.Empty;
        public string UnHealthyColor { get; set; } = "#c74f4f";
        public string HealthyColor { get; set; } = "#91c337";
        public string DegradedColor { get; set; } = "#ffc107";
    }

    public class TeamsHealthCheckPublisherOptionsValidator : AbstractValidator<TeamsHealthCheckPublisherOptions>
    {
        public TeamsHealthCheckPublisherOptionsValidator()
        {
            RuleFor(o => o.WebHookUrl).NotEmpty().WithMessage("Teams web hook url can't be empty");
        }
    }
}
