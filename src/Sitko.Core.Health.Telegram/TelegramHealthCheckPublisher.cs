using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Sitko.Core.Health.Telegram
{
    public class TelegramHealthCheckPublisher : BaseHealthCheckPublisher<TelegramHealthCheckPublisherOptions>
    {
        private readonly ChatId _chatId;
        private readonly ITelegramBotClient _telegramBotClient;

        public TelegramHealthCheckPublisher(IOptionsMonitor<TelegramHealthCheckPublisherOptions> options,
            ILogger<TelegramHealthCheckPublisher> logger, IHostEnvironment hostingEnvironment,
            IHttpClientFactory httpClientFactory) : base(options, logger, hostingEnvironment)
        {
            _chatId = new ChatId(Options.ChatId);
            _telegramBotClient = new TelegramBotClient(Options.Token,
                httpClientFactory.CreateClient());
        }

        protected override Task DoSendAsync(string checkName, HealthReportEntry entry,
            CancellationToken cancellationToken)
        {
            var serviceName = $"{HostingEnvironment.ApplicationName} ({Dns.GetHostName()})";
            string title = entry.Status switch
            {
                HealthStatus.Unhealthy => $"Error in {serviceName}. Check: {checkName}",
                HealthStatus.Degraded => $"Check {checkName} in {serviceName} is degraded",
                HealthStatus.Healthy => $"Check {checkName} in {serviceName} is restored",
                _ => $"Unknown check {checkName} status {entry.Status}"
            };
            string summary = string.IsNullOrEmpty(entry.Description) ? "No description" : entry.Description;


            string text = $"{TelegramExtensions.TelegramRaw(title)}\n{TelegramExtensions.TelegramRaw(summary)}";

            if (entry.Status == HealthStatus.Unhealthy || entry.Status == HealthStatus.Degraded)
            {
                if (entry.Exception != null)
                {
                    text += $"\n`{entry.Exception}`";
                }
            }

            return _telegramBotClient.SendTextMessageAsync(_chatId,
                text, ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
    }
}
