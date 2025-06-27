using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Sitko.Core.Health.Telegram;

public class TelegramHealthCheckPublisher : BaseHealthCheckPublisher<TelegramHealthReporterModuleOptions>
{
    private readonly ChatId chatId;
    private readonly ITelegramBotClient telegramBotClient;

    public TelegramHealthCheckPublisher(IOptionsMonitor<TelegramHealthReporterModuleOptions> options,
        ILogger<TelegramHealthCheckPublisher> logger, IApplicationContext applicationContext,
        IHttpClientFactory httpClientFactory) : base(options, logger, applicationContext)
    {
        chatId = new ChatId(Options.ChatId);
        telegramBotClient = new TelegramBotClient(Options.Token,
            httpClientFactory.CreateClient());
    }

    protected override Task DoSendAsync(string checkName, HealthReportEntry entry,
        CancellationToken cancellationToken)
    {
        var serviceName = $"{ApplicationContext.Name} ({Dns.GetHostName()})";
        var title = entry.Status switch
        {
            HealthStatus.Unhealthy => $"Error in {serviceName}. Check: {checkName}",
            HealthStatus.Degraded => $"Check {checkName} in {serviceName} is degraded",
            HealthStatus.Healthy => $"Check {checkName} in {serviceName} is restored",
            _ => $"Unknown check {checkName} status {entry.Status}"
        };
        var summary = string.IsNullOrEmpty(entry.Description) ? "No description" : entry.Description;


        var text = $"{TelegramExtensions.TelegramRaw(title)}\n{TelegramExtensions.TelegramRaw(summary)}";

        if (entry.Status == HealthStatus.Unhealthy || entry.Status == HealthStatus.Degraded)
        {
            if (entry.Exception != null)
            {
                text += $"\n`{entry.Exception}`";
            }
        }

        return telegramBotClient.SendMessage(chatId,
            text, ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }
}

