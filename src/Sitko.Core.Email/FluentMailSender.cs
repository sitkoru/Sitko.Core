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

        public async Task<bool> SendHtmlMailAsync<T>(MailEntry<T> mailEntry, string template)
        {
            var html = await _renderer.RenderViewToStringAsync(template, mailEntry);
            return await SendMailAsync(mailEntry, html);
        }

        public async Task<bool> SendHtmlMailAsync(MailEntry mailEntry, string template)
        {
            var html = await _renderer.RenderViewToStringAsync(template, mailEntry);
            return await SendMailAsync(mailEntry, html);
        }

        public async Task<bool> SendMailAsync(MailEntry mailEntry, string body)
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

            var success = true;
            foreach (var recipient in mailEntry.Recipients)
            {
                try
                {
                    message.Data.ToAddresses.Clear();
                    message.To(recipient);
                    var res = await message.SendAsync();
                    if (!res.Successful)
                    {
                        foreach (var errorMessage in res.ErrorMessages)
                        {
                            _logger.LogError("Error while sending email {subject} to {recipient}: {errorText}",
                                mailEntry.Subject, recipient, errorMessage);
                        }

                        success = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    throw;
                }
            }

            return success;
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
