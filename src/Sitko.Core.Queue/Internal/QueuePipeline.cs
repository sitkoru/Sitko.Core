namespace Sitko.Core.Queue.Internal;

internal sealed class QueuePipeline
{
    private IQueueMiddleware? mw;

    public void Use(IQueueMiddleware middleware)
    {
        if (mw == null)
        {
            mw = middleware;
        }
        else
        {
            mw.SetNext(middleware);
        }
    }

    public Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext,
        Func<T, QueueMessageContext, Task<QueuePublishResult>> callback) where T : class =>
        mw == null ? callback(message, messageContext) : mw.PublishAsync(message, messageContext, callback);

    public Task<bool> ReceiveAsync<T>(T message, QueueMessageContext messageContext,
        Func<T, QueueMessageContext, Task<bool>> callback)
        where T : class =>
        mw == null ? callback(message, messageContext) : mw.ReceiveAsync(message, messageContext, callback);
}

