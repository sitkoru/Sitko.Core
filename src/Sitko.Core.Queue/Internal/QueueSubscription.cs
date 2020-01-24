using System;
using System.Threading.Tasks;

namespace Sitko.Core.Queue.Internal
{
    internal abstract class QueueSubscription
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    internal class QueueSubscription<T> : QueueSubscription where T : class, new()
    {
        public QueueSubscription(Func<QueuePayload<T>, Task<bool>> callback)
        {
            Callback = callback;
        }


        public Task<bool> ProcessAsync(QueuePayload<T> payload)
        {
            return Callback(payload);
        }

        private Func<QueuePayload<T>, Task<bool>> Callback { get; }
    }
}
