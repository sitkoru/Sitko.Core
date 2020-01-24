using System;
using System.Threading.Tasks;

namespace Sitko.Core.Queue
{
    public interface IQueue : IAsyncDisposable
    {
        Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext? parentMessageContext = null)
            where T : class, new();

        Task<QueueSubscribeResult> SubscribeAsync<T>(
            Func<T, QueueMessageContext, Task<bool>> callback)
            where T : class, new();

        Task UnsubscribeAsync<T>(Guid subscriptionId) where T : class, new();

        Task<(TResponse message, QueueMessageContext messageContext)> RequestAsync<TMessage, TResponse>(
            TMessage message,
            QueueMessageContext? parentMessageContext = null, TimeSpan? timeout = null)
            where TMessage : class, new()
            where TResponse : class, new();

        Task<QueueSubscribeResult> ReplyAsync<TMessage, TResponse>(
            Func<TMessage, QueueMessageContext, Task<TResponse>> callback)
            where TMessage : class, new()
            where TResponse : class, new();

        Task<bool> StopReplyAsync<TMessage, TResponse>(Guid id) 
            where TMessage : class, new()
            where TResponse : class, new();
    }
}
