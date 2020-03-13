using System.Threading.Tasks;

namespace Sitko.Core.Queue.Internal
{
    internal class QueuePipeline
    {
        private IQueueMiddleware? _mw;

        public void Use(IQueueMiddleware middleware)
        {
            if (_mw == null)
            {
                _mw = middleware;
            }
            else
            {
                _mw.SetNext(middleware);
            }
        }

        public Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext,
            PublishAsyncDelegate<T> callback) where T : class
        {
            if (_mw == null)
            {
                return callback(message, messageContext);
            }

            return _mw.PublishAsync(message, messageContext, callback);
        }

        public Task<bool> ReceiveAsync<T>(T message, QueueMessageContext messageContext,
            ReceiveAsyncDelegate<T> callback)
            where T : class
        {
            if (_mw == null)
            {
                return callback(message, messageContext);
            }

            return _mw.ReceiveAsync(message, messageContext, callback);
        }
    }
}
