namespace Sitko.Core.Queue.Internal;

internal abstract class QueueSubscription
{
    public Guid Id { get; } = Guid.NewGuid();
}

internal sealed class QueueSubscription<T> : QueueSubscription where T : class
{
    public QueueSubscription(Func<T, QueueMessageContext, Task<bool>> callback) => Callback = callback;

    private Func<T, QueueMessageContext, Task<bool>> Callback { get; }


    public Task<bool> ProcessAsync(T message, QueueMessageContext context) => Callback(message, context);
}

