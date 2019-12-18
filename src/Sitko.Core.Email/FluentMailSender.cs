using System;
using System.Threading.Tasks;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using Hangfire;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Email
{
    public class FluentMailSender : IMailSender
    {
        private readonly IFluentEmailFactory _emailFactory;
        private readonly ViewToStringRendererService _renderer;
        private readonly ILogger<FluentMailSender> _logger;
        private readonly IBackgroundJobClient? _backgroundJobClient;

        public FluentMailSender(IFluentEmailFactory emailFactory, ViewToStringRendererService renderer,
            ILogger<FluentMailSender> logger, IBackgroundJobClient? backgroundJobClient = null)
        {
            _emailFactory = emailFactory;
            _renderer = renderer;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
        }

        public Task SendHtmlMailAsync<T>(MailEntry<T> mailEntry, string template)
        {
            var html = _renderer.RenderViewToStringAsync(template, mailEntry).GetAwaiter().GetResult();
            return SendMailAsync(mailEntry, html);
        }

        public async Task SendHtmlMailAsync(MailEntry mailEntry, string template)
        {
            var html = await _renderer.RenderViewToStringAsync(template, mailEntry);
            await SendMailAsync(mailEntry, html);
        }

        public async Task SendMailAsync(MailEntry mailEntry, string body)
        {
            var message = _emailFactory.Create().Subject(mailEntry.Subject);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(body);

            message.Body(body, true);
            message.PlaintextAlternativeBody(htmlDoc.DocumentNode.SelectSingleNode("//body")?.InnerText);

            foreach (var attachment in mailEntry.Attachments)
            {
                message.Attach(new Attachment
                {
                    Data = attachment.Data, Filename = attachment.Name, ContentType = attachment.Type
                });
            }

            foreach (var recipient in mailEntry.Recipients)
            {
                try
                {
                    message.To(recipient);
                    var res = await message.SendAsync();
                    if (!res.Successful)
                    {
                        foreach (var errorMessage in res.ErrorMessages)
                        {
                            _logger.LogError("Error while sending email {subject} to {recipient}: {errorText}",
                                mailEntry.Subject, recipient, errorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    throw;
                }
            }
        }

        public void SendInBackground<T>(MailEntry<T> mailEntry, string template)
        {
            if (_backgroundJobClient != null)
            {
                _backgroundJobClient.Enqueue(() => SendHtmlMailAsync(mailEntry, template));
            }
            else
            {
                throw new Exception("No background client!");
            }
        }
    }
}
