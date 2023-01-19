namespace Sitko.Core.Queue;

public abstract class BaseQueueMiddleware : IQueueMiddleware
{
    public IQueueMiddleware? Next { get; private set; }

    public void SetNext(IQueueMiddleware next)
    {
        {
            if (Next == null)
            {
                Next = next;
            }
            else
            {
                Next.SetNext(next);
            }
        }
    }

    public virtual Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext,
        Func<T, QueueMessageContext, Task<QueuePublishResult>>? callback = null) where T : class
    {
        if (Next != null)
        {
            return Next.PublishAsync(message, messageContext, callback);
        }

        if (callback != null)
        {
            return callback(message, messageContext);
        }

        var result = new QueuePublishResult();
        result.SetError("Message reached end of pipeline");
        return Task.FromResult(result);
    }

    public virtual Task<bool> ReceiveAsync<T>(T message, QueueMessageContext messageContext,
        Func<T, QueueMessageContext, Task<bool>>? callback = null) where T : class
    {
        if (Next != null)
        {
            return Next.ReceiveAsync(message, messageContext, callback);
        }

        return callback != null ? callback(message, messageContext) : Task.FromResult(false);
    }
}

