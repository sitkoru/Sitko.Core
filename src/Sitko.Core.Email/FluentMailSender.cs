using FluentEmail.Core;
using FluentEmail.Core.Models;
using Hangfire;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Razor.Templating.Core;
using Sitko.Core.App.Results;

namespace Sitko.Core.Email;

public class FluentMailSender<TOptions> : IMailSender where TOptions : EmailModuleOptions
{
    private readonly IBackgroundJobClient? backgroundJobClient;
    private readonly IFluentEmailFactory emailFactory;
    private readonly ILogger<FluentMailSender<TOptions>> logger;
    private readonly HtmlRenderer htmlRenderer;

    public FluentMailSender(IFluentEmailFactory emailFactory,
        ILogger<FluentMailSender<TOptions>> logger, HtmlRenderer htmlRenderer,
        IBackgroundJobClient? backgroundJobClient = null)
    {
        this.emailFactory = emailFactory;
        this.logger = logger;
        this.htmlRenderer = htmlRenderer;
        this.backgroundJobClient = backgroundJobClient;
    }

    public async Task<IOperationResult> SendHtmlMailAsync<T>(MailEntry<T> mailEntry, string templatePath)
    {
        var html = await RazorTemplateEngine.RenderAsync(templatePath, mailEntry);
        return await SendMailAsync(mailEntry, html);
    }

    public async Task<IOperationResult> SendHtmlMailAsync(MailEntry mailEntry, string templatePath)
    {
        var html = await RazorTemplateEngine.RenderAsync(templatePath, mailEntry);
        return await SendMailAsync(mailEntry, html);
    }

    public async Task<IOperationResult> SendHtmlMailAsync<T>(MailEntry mailEntry, Dictionary<string, object?> data) where T : IComponent
    {
        var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(data);
            var output = await htmlRenderer.RenderComponentAsync<T>(parameters);

            return output.ToHtmlString();
        });
        return await SendMailAsync(mailEntry, html);
    }

    public async Task<IOperationResult> SendMailAsync(MailEntry mailEntry, string body)
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

        var errors = new List<string>();
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
                        errors.Add($"Error while sending email {mailEntry.Subject} to {recipient}: {errorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while sending email {Subject} to {Recipient}: {ErrorText}",
                    mailEntry.Subject, recipient, ex.ToString());
                return new OperationResult(ex,
                    $"Error while sending email {mailEntry.Subject} to {recipient}: {ex}");
            }
        }

        return errors.Any() ? new OperationResult($"Errors: {string.Join(". ", errors)}") : new OperationResult();
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

    public void SendInBackground<T>(MailEntry mailEntry, Dictionary<string, object?> data)
        where T : IComponent
    {
        if (backgroundJobClient != null)
        {
            backgroundJobClient.Enqueue(() => SendHtmlMailAsync<T>(mailEntry, data));
        }
        else
        {
            throw new InvalidOperationException("No background client!");
        }
    }
}

