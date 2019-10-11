using System.Threading.Tasks;
using Google.Protobuf;
using Sitko.Core.PersistentQueue.Common;

namespace Sitko.Core.PersistentQueue.HostedService
{
    public interface IPersistentQueueMessageProcessor
    {
    }

    public interface IPersistentQueueMessageProcessor<T> : IPersistentQueueMessageProcessor where T : IMessage, new()
    {
        Task<bool> ProcessAsync(T message, PersistentQueueMessageContext context);
    }
}
