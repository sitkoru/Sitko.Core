using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sitko.Core.MessageBus
{
    public interface IMessageBus
    {
        void Publish<T>(T message) where T : IMessage;
        Guid SubscribeAll(Func<IMessage, CancellationToken?, Task<bool>> callback);
        Guid Subscribe<T>(Func<T, CancellationToken?, Task<bool>> callback) where T : IMessage;
        void Unsubscribe(Guid subscriptionId);
        void Start(CancellationToken? cancellationToken);
        Task StopAsync(CancellationToken? cancellationToken);
    }
}
