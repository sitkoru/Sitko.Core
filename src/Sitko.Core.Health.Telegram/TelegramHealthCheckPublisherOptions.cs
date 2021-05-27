using System.Collections.Generic;
using Sitko.Core.App;

namespace Sitko.Core.Health.Telegram
{
    public class TelegramHealthCheckPublisherOptions : BaseModuleConfig
    {
        public string Token { get; set; } = string.Empty;
        public long ChatId { get; set; } = 0;

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(Token))
                {
                    return (false, new[] {"Telegram token can't be empty"});
                }

                if (ChatId == 0)
                {
                    return (false, new[] {"Telegram chat id can't be 0"});
                }
            }

            return result;
        }
    }
}
