using System.Threading.Tasks;

namespace Sitko.Core.Queue.Internal
{
    internal class QueuePipeline
    {
        private IQueueMiddleware? mw;

        public void Use(IQueueMiddleware middleware)
        {
            if (mw == null)
            {
                mw = middleware;
            }
            else
            {
                mw.SetNext(middleware);
            }
        }

        public Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext,
            PublishAsyncDelegate<T> callback) where T : class
        {
            if (mw == null)
            {
                return callback(message, messageContext);
            }

            return mw.PublishAsync(message, messageContext, callback);
        }

        public Task<bool> ReceiveAsync<T>(T message, QueueMessageContext messageContext,
            ReceiveAsyncDelegate<T> callback)
            where T : class
        {
            if (mw == null)
            {
                return callback(message, messageContext);
            }

            return mw.ReceiveAsync(message, messageContext, callback);
        }
    }
}
