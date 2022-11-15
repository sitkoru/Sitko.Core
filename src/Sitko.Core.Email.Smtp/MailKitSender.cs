using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Sitko.Core.Email.Smtp;

/// <summary>
///     Send emails with the MailKit Library.
/// </summary>
public class MailKitSender : ISender
{
    private readonly IOptionsMonitor<SmtpEmailModuleOptions> smtpClientOptionsMonitor;

    /// <summary>
    ///     Creates a sender that uses the given SmtpClientOptions when sending with MailKit. Since the client is internal this
    ///     will dispose of the client.
    /// </summary>
    /// <param name="smtpClientOptions">The SmtpClientOptions to use to create the MailKit client</param>
    public MailKitSender(IOptionsMonitor<SmtpEmailModuleOptions> smtpClientOptions) =>
        smtpClientOptionsMonitor = smtpClientOptions;

    private SmtpEmailModuleOptions SmtpClientOptions => smtpClientOptionsMonitor.CurrentValue;

    /// <summary>
    ///     Send the specified email.
    /// </summary>
    /// <returns>A response with any errors and a success boolean.</returns>
    /// <param name="email">Email.</param>
    /// <param name="token">Cancellation Token.</param>
    public SendResponse Send(IFluentEmail email, CancellationToken? token = null)
    {
        var response = new SendResponse();
        var message = CreateMailMessage(email);

        if (token?.IsCancellationRequested ?? false)
        {
            response.ErrorMessages.Add("Message was cancelled by cancellation token.");
            return response;
        }

        try
        {
            using (var client = new SmtpClient())
            {
                client.Connect(
                    SmtpClientOptions.Server,
                    SmtpClientOptions.Port,
                    SmtpClientOptions.SocketOptions,
                    token.GetValueOrDefault());

                // Note: only needed if the SMTP server requires authentication
                if (!string.IsNullOrEmpty(SmtpClientOptions.UserName))
                {
                    client.Authenticate(SmtpClientOptions.UserName, SmtpClientOptions.Password,
                        token.GetValueOrDefault());
                }

                client.Send(message, token.GetValueOrDefault());
                client.Disconnect(true, token.GetValueOrDefault());
            }
        }
        catch (Exception ex)
        {
            response.ErrorMessages.Add(ex.Message);
        }

        return response;
    }

    /// <summary>
    ///     Send the specified email.
    /// </summary>
    /// <returns>A response with any errors and a success boolean.</returns>
    /// <param name="email">Email.</param>
    /// <param name="token">Cancellation Token.</param>
    public async Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null)
    {
        var response = new SendResponse();
        var message = CreateMailMessage(email);

        if (token?.IsCancellationRequested ?? false)
        {
            response.ErrorMessages.Add("Message was cancelled by cancellation token.");
            return response;
        }

        try
        {
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(
                    SmtpClientOptions.Server,
                    SmtpClientOptions.Port,
                    SmtpClientOptions.SocketOptions,
                    token.GetValueOrDefault());

                // Note: only needed if the SMTP server requires authentication
                if (!string.IsNullOrEmpty(SmtpClientOptions.UserName))
                {
                    await client.AuthenticateAsync(SmtpClientOptions.UserName, SmtpClientOptions.Password,
                        token.GetValueOrDefault());
                }

                await client.SendAsync(message, token.GetValueOrDefault());
                await client.DisconnectAsync(true, token.GetValueOrDefault());
            }
        }
        catch (Exception ex)
        {
            response.ErrorMessages.Add(ex.Message);
        }

        return response;
    }

    /// <summary>
    ///     Create a MimMessage so MailKit can send it
    /// </summary>
    /// <returns>The mail message.</returns>
    /// <param name="email">Email data.</param>
    private static MimeMessage CreateMailMessage(IFluentEmail email)
    {
        var data = email.Data;

        MimeMessage message = new() { Subject = data.Subject ?? string.Empty };

        message.From.Add(new MailboxAddress(data.FromAddress.Name, data.FromAddress.EmailAddress));

        var builder = new BodyBuilder();
        if (!string.IsNullOrEmpty(data.PlaintextAlternativeBody))
        {
            builder.TextBody = data.PlaintextAlternativeBody;
            builder.HtmlBody = data.Body;
        }
        else if (!data.IsHtml)
        {
            builder.TextBody = data.Body;
        }
        else
        {
            builder.HtmlBody = data.Body;
        }

        data.Attachments.ForEach(x =>
        {
            builder.Attachments.Add(x.Filename, x.Data, ContentType.Parse(x.ContentType));
        });


        message.Body = builder.ToMessageBody();

        foreach (var header in data.Headers)
        {
            message.Headers.Add(header.Key, header.Value);
        }

        data.ToAddresses.ForEach(x => { message.To.Add(new MailboxAddress(x.Name, x.EmailAddress)); });

        data.CcAddresses.ForEach(x => { message.Cc.Add(new MailboxAddress(x.Name, x.EmailAddress)); });

        data.BccAddresses.ForEach(x => { message.Bcc.Add(new MailboxAddress(x.Name, x.EmailAddress)); });

        data.ReplyToAddresses.ForEach(x => { message.ReplyTo.Add(new MailboxAddress(x.Name, x.EmailAddress)); });

        switch (data.Priority)
        {
            case Priority.Low:
                message.Priority = MessagePriority.NonUrgent;
                break;
            case Priority.Normal:
                message.Priority = MessagePriority.Normal;
                break;
            case Priority.High:
                message.Priority = MessagePriority.Urgent;
                break;
        }

        return message;
    }
}

