using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sitko.Core.Health;

namespace Sitko.Core.Queue
{
    public interface IQueue : IAsyncDisposable
    {
        Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext? parentMessageContext = null)
            where T : class;

        Task<QueueSubscribeResult> SubscribeAsync<T>(
            Func<T, QueueMessageContext, Task<bool>> callback)
            where T : class;

        Task UnsubscribeAsync<T>(Guid subscriptionId) where T : class;

        Task<(TResponse message, QueueMessageContext messageContext)> RequestAsync<TMessage, TResponse>(
            TMessage message,
            QueueMessageContext? parentMessageContext = null, TimeSpan? timeout = null)
            where TMessage : class
            where TResponse : class;

        Task<QueueSubscribeResult> ReplyAsync<TMessage, TResponse>(
            Func<TMessage, QueueMessageContext, Task<TResponse>> callback)
            where TMessage : class
            where TResponse : class;

        Task<bool> StopReplyAsync<TMessage, TResponse>(Guid id)
            where TMessage : class
            where TResponse : class;

        Task<(HealthStatus status, string? errorMessage)> CheckHealthAsync();
    }
}
