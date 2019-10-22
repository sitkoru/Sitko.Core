using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Sitko.Core.Health.Telegram
{
    public class TelegramHealthCheckPublisher : IHealthCheckPublisher
    {
        private readonly ChatId _chatId;
        private readonly Dictionary<string, HealthReportEntry> _entries = new Dictionary<string, HealthReportEntry>();
        private readonly IHostEnvironment _hostingEnvironment;
        private readonly ILogger<TelegramHealthCheckPublisher> _logger;
        private readonly ITelegramBotClient _telegramBotClient;

        [SuppressMessage("ReSharper", "IDISP004")]
        public TelegramHealthCheckPublisher(TelegramHealthCheckPublisherOptions options,
            ILogger<TelegramHealthCheckPublisher> logger,
            IHttpClientFactory httpClientFactory,
            IHostEnvironment hostingEnvironment)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
            _chatId = new ChatId(options.ChatId);
            _telegramBotClient = new TelegramBotClient(options.Token,
                httpClientFactory.CreateClient());
        }


        public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            foreach (var entry in report.Entries)
            {
                bool isNew = false;
                if (!_entries.ContainsKey(entry.Key))
                {
                    _entries.Add(entry.Key, entry.Value);
                    isNew = true;
                }

                if (isNew || _entries[entry.Key].Status != entry.Value.Status)
                {
                    string text;


                    if (entry.Value.Status == HealthStatus.Healthy)
                    {
                        if (isNew)
                        {
                            continue;
                        }

                        text = $"*{_hostingEnvironment.ApplicationName} ({Dns.GetHostName()})*\n" +
                               $"Check {entry.Key} восстановился";
                    }
                    else
                    {
                        text = $"*Ошибка в сервисе {_hostingEnvironment.ApplicationName} ({Dns.GetHostName()})*\n" +
                               $"Check: {entry.Key}\n";

                        if (!string.IsNullOrEmpty(entry.Value.Description))
                        {
                            text += $"Описание: {entry.Value.Description}\n";
                        }

                        if (entry.Value.Exception != null)
                        {
                            text += $"Исключение: `{entry.Value.Exception}`\n";
                        }
                    }

                    try
                    {
                        await _telegramBotClient.SendTextMessageAsync(_chatId,
                            text, ParseMode.Markdown,
                            cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, ex.ToString());
                    }
                }

                _entries[entry.Key] = entry.Value;
            }
        }
    }
}
