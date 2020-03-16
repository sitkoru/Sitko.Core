namespace Sitko.Core.Health.Telegram
{
    public class TelegramHealthCheckPublisherOptions
    {
        public string Token { get; set; } = string.Empty;
        public long ChatId { get; set; } = 0;
    }
}
