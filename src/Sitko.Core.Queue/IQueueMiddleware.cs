namespace Sitko.Core.Queue;

public interface IQueueMiddleware
{
    void SetNext(IQueueMiddleware next);

    Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext,
        Func<T, QueueMessageContext, Task<QueuePublishResult>>? callback = null) where T : class;

    Task<bool> ReceiveAsync<T>(T message, QueueMessageContext messageContext,
        Func<T, QueueMessageContext, Task<bool>>? callback = null) where T : class;
}

