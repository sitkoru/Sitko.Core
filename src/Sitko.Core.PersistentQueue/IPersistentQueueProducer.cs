using System.Threading.Tasks;
using Google.Protobuf;

namespace Sitko.Core.PersistentQueue
{
    public interface IPersistentQueueProducer<in T> where T : IMessage
    {
        void Produce(T message, PersistentQueueMessageContext context = null);

        Task<(TResponse response, PersistentQueueMessageContext responseContext)> RequestAsync<TResponse>(T message,
            PersistentQueueMessageContext context = null, int offset = 5000)
            where TResponse : class, IMessage, new();
    }
}