using System.Threading;
using System.Threading.Tasks;

namespace Sitko.Core.MessageBus
{
    public interface IMessageBusSubscription
    {
        Task<bool> ProcessAsync(IMessage message, CancellationToken? cancellationToken);
        bool CanProcess(IMessage message);
    }
}
