using System.Threading.Tasks;
using Sitko.Core.App.Results;

namespace Sitko.Core.Email;

public interface IMailSender
{
    Task<IOperationResult> SendHtmlMailAsync<T>(MailEntry<T> mailEntry, string templatePath);
    Task<IOperationResult> SendHtmlMailAsync(MailEntry mailEntry, string templatePath);
    Task<IOperationResult> SendMailAsync(MailEntry mailEntry, string body);
    void SendInBackground<T>(MailEntry<T> mailEntry, string templatePath);
}
