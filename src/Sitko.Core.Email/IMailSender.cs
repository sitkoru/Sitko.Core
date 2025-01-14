using Microsoft.AspNetCore.Components;
using Sitko.Core.App.Results;

namespace Sitko.Core.Email;

public interface IMailSender
{
    Task<IOperationResult> SendHtmlMailAsync<T>(MailEntry<T> mailEntry, string templatePath);
    Task<IOperationResult> SendHtmlMailAsync(MailEntry mailEntry, string templatePath);
    Task<IOperationResult> SendHtmlMailAsync<T>(MailEntry mailEntry, Dictionary<string, object?> data) where T : IComponent;
    Task<IOperationResult> SendMailAsync(MailEntry mailEntry, string body);
    void SendInBackground<T>(MailEntry<T> mailEntry, string templatePath);
    void SendInBackground<T>(MailEntry mailEntry, Dictionary<string, object?> data) where T : IComponent;
}

