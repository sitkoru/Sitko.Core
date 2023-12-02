namespace Sitko.Core.Queue;

public interface IQueueProcessor<in T> : IQueueProcessor where T : class
{
    Task<bool> ProcessAsync(T message, QueueMessageContext messageContext);
}

public interface IQueueProcessor;

