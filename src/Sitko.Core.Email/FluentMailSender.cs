using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using Hangfire;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Web.Razor;

namespace Sitko.Core.Email
{
    public class FluentMailSender<TOptions> : IMailSender where TOptions : EmailModuleOptions
    {
        private readonly IFluentEmailFactory emailFactory;
        private readonly ViewToStringRendererService<TOptions> renderer;
        private readonly ILogger<FluentMailSender<TOptions>> logger;
        private readonly IBackgroundJobClient? backgroundJobClient;

        public FluentMailSender(IFluentEmailFactory emailFactory,
            ViewToStringRendererService<TOptions> renderer,
            ILogger<FluentMailSender<TOptions>> logger, IBackgroundJobClient? backgroundJobClient = null)
        {
            this.emailFactory = emailFactory;
            this.renderer = renderer;
            this.logger = logger;
            this.backgroundJobClient = backgroundJobClient;
        }

        public async Task<bool> SendHtmlMailAsync<T>(MailEntry<T> mailEntry, string templatePath)
        {
            var html = await renderer.RenderViewToStringAsync(templatePath, mailEntry);
            return await SendMailAsync(mailEntry, html);
        }

        public async Task<bool> SendHtmlMailAsync(MailEntry mailEntry, string templatePath)
        {
            var html = await renderer.RenderViewToStringAsync(templatePath, mailEntry);
            return await SendMailAsync(mailEntry, html);
        }

        public async Task<bool> SendMailAsync(MailEntry mailEntry, string body)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(body);
            var plainText = htmlDoc.DocumentNode.SelectSingleNode("//body")?.InnerText;

            var attachments = new List<Attachment>();
            foreach (var attachment in mailEntry.Attachments)
            {
                var stream = attachment.Data;
                if (!stream.CanSeek)
                {
                    var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    stream = ms;
                }

                attachments.Add(new Attachment
                {
                    Data = stream, Filename = attachment.Name, ContentType = attachment.Type
                });
            }

            var success = true;
            foreach (var recipient in mailEntry.Recipients)
            {
                try
                {
                    var message = emailFactory.Create().Subject(mailEntry.Subject);
                    message.Body(body, true);
                    message.PlaintextAlternativeBody(plainText);

                    foreach (var attachment in attachments)
                    {
                        attachment.Data.Position = 0;
                        message.Attach(attachment);
                    }

                    message.To(recipient);
                    var res = await message.SendAsync();
                    if (!res.Successful)
                    {
                        foreach (var errorMessage in res.ErrorMessages)
                        {
                            logger.LogError("Error while sending email {Subject} to {Recipient}: {ErrorText}",
                                mailEntry.Subject, recipient, errorMessage);
                        }

                        success = false;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while sending email {Subject} to {Recipient}: {ErrorText}",
                        mailEntry.Subject, recipient, ex.ToString());
                    throw;
                }
            }

            return success;
        }

        public void SendInBackground<T>(MailEntry<T> mailEntry, string templatePath)
        {
            if (backgroundJobClient != null)
            {
                backgroundJobClient.Enqueue(() => SendHtmlMailAsync(mailEntry, templatePath));
            }
            else
            {
                throw new InvalidOperationException("No background client!");
            }
        }
    }
}
