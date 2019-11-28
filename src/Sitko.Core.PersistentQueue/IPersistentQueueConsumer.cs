using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Sitko.Core.PersistentQueue.HostedService;

namespace Sitko.Core.PersistentQueue
{
    public interface IPersistentQueueConsumer
    {
    }

    public interface IPersistentQueueConsumer<TMessage> : IPersistentQueueConsumer
        where TMessage : IMessage, new()
    {
        Task RunAsync(Func<TMessage, PersistentQueueMessageContext, Task<bool>> callback,
            PersistedQueueHostedServiceOptions<TMessage> options = null);

        Task RunWithResponseAsync<TResponse>(
            Func<TMessage, PersistentQueueMessageContext, Task<(bool isSuccess, TResponse response)>> callback,
            PersistedQueueHostedServiceOptions<TMessage> options = null)
            where TResponse : IMessage, new();
    }
}
