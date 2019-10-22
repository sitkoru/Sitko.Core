using System;

namespace Sitko.Core.Health.Telegram
{
    public class TelegramHealthCheckPublisherOptions
    {
        public string Token { get; }
        public long ChatId { get; }

        public TelegramHealthCheckPublisherOptions(string token, long chatId)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Telegram token can't be empty", nameof(token));
            }

            Token = token;

            if (chatId == 0)
            {
                throw new ArgumentException("Telegram chat id can't be 0", nameof(token));
            }

            ChatId = chatId;
        }
    }
}
