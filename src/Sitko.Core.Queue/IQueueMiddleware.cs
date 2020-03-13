using System.Threading.Tasks;

namespace Sitko.Core.Queue
{
    public delegate Task<QueuePublishResult> PublishAsyncDelegate<T>(T message, QueueMessageContext messageContext)
        where T : class;

    public delegate Task<bool> ReceiveAsyncDelegate<T>(T message, QueueMessageContext messageContext) where T : class;

    public interface IQueueMiddleware
    {
        void SetNext(IQueueMiddleware next);

        Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext,
            PublishAsyncDelegate<T>? callback = null) where T : class;

        Task<bool> ReceiveAsync<T>(T message, QueueMessageContext messageContext,
            ReceiveAsyncDelegate<T>? callback = null) where T : class;
    }
}
