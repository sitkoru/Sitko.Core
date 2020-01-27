using System.Threading.Tasks;

namespace Sitko.Core.Email
{
    public interface IMailSender
    {
        Task SendHtmlMailAsync<T>(MailEntry<T> mailEntry, string template);
        Task SendHtmlMailAsync(MailEntry mailEntry, string template);
        Task SendMailAsync(MailEntry mailEntry, string body);
        void SendInBackground<T>(MailEntry<T> mailEntry, string template);
    }
}
