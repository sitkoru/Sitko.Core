using System.Threading.Tasks;

namespace Sitko.Core.Email
{
    public interface IMailSender
    {
        Task<bool> SendHtmlMailAsync<T>(MailEntry<T> mailEntry, string template);
        Task<bool> SendHtmlMailAsync(MailEntry mailEntry, string template);
        Task<bool> SendMailAsync(MailEntry mailEntry, string body);
        void SendInBackground<T>(MailEntry<T> mailEntry, string template);
    }
}
