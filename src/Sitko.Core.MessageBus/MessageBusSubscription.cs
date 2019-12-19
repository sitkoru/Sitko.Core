using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sitko.Core.MessageBus
{
    public class MessageBusSubscription : IMessageBusSubscription
    {
        public MessageBusSubscription(Func<IMessage, CancellationToken?, Task<bool>> callback)
        {
            Callback = callback;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public Task<bool> ProcessAsync(IMessage message, CancellationToken? cancellationToken)
        {
            return Callback(message, cancellationToken);
        }

        public bool CanProcess(IMessage message)
        {
            return true;
        }

        private Func<IMessage, CancellationToken?, Task<bool>> Callback { get; }
    }
    
    class MessageBusSubscription<T> : IMessageBusSubscription where T : IMessage
    {
        public MessageBusSubscription(Func<T, CancellationToken?, Task<bool>> callback)
        {
            Callback = callback;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public Task<bool> ProcessAsync(IMessage message, CancellationToken? cancellationToken)
        {
            if (message is T typedMessage)
            {
                return Callback(typedMessage, cancellationToken);
            }

            return Task.FromResult(false);
        }

        public bool CanProcess(IMessage message)
        {
            return message is T;
        }

        private Func<T, CancellationToken?, Task<bool>> Callback { get; }
    }
}
