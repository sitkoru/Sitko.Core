using System.Threading.Tasks;

namespace Sitko.Core.Email
{
    public interface IMailSender
    {
        Task<bool> SendHtmlMailAsync<T>(MailEntry<T> mailEntry, string templatePath);
        Task<bool> SendHtmlMailAsync(MailEntry mailEntry, string templatePath);
        Task<bool> SendMailAsync(MailEntry mailEntry, string body);
        void SendInBackground<T>(MailEntry<T> mailEntry, string templatePath);
    }
}
