using FluentValidation;
using Sitko.Core.App;

namespace Sitko.Core.Health.Telegram
{
    public class TelegramHealthCheckPublisherOptions : BaseModuleOptions
    {
        public string Token { get; set; } = string.Empty;
        public long ChatId { get; set; } = 0;
    }

    public class TelegramHealthCheckPublisherOptionsValidator : AbstractValidator<TelegramHealthCheckPublisherOptions>
    {
        public TelegramHealthCheckPublisherOptionsValidator()
        {
            RuleFor(o => o.Token).NotEmpty().WithMessage("Telegram token can't be empty");
            RuleFor(o => o.ChatId).NotEqual(0).WithMessage("Telegram chat id can't be 0");
        }
    }
}
