using System;
using System.Threading.Tasks;
using Google.Protobuf;

namespace Sitko.Core.PersistentQueue
{
    public interface IPersistentQueueConsumer<TMessage> : IDisposable where TMessage : IMessage, new()
    {
        Task RunAsync(Func<TMessage, PersistentQueueMessageContext, Task<bool>> callback);

        Task RunWithResponseAsync<TResponse>(
            Func<TMessage, PersistentQueueMessageContext, Task<(bool isSuccess, TResponse response)>> callback)
            where TResponse : IMessage, new();

        Task StopAsync();
    }
}
