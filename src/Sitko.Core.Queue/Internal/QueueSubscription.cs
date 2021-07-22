using System;
using System.Threading.Tasks;

namespace Sitko.Core.Queue.Internal
{
    internal abstract class QueueSubscription
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    internal class QueueSubscription<T> : QueueSubscription where T : class
    {
        public QueueSubscription(Func<T, QueueMessageContext, Task<bool>> callback) => Callback = callback;


        public Task<bool> ProcessAsync(T message, QueueMessageContext context) => Callback(message, context);

        private Func<T, QueueMessageContext, Task<bool>> Callback { get; }
    }
}
