namespace Sitko.Core.Queue.Kafka;

[AttributeUsage(AttributeTargets.Class)]
public class QueueHandlerAttribute(
    string group,
    int parallelThreadCount = 20,
    int bufferSize = 10,
    ConsumerGroupRetryStrategy retryStrategy = ConsumerGroupRetryStrategy.Simple) : Attribute
{
    public string Group { get; } = group;
    public int ParallelThreadCount { get; } = parallelThreadCount;
    public int BufferSize { get; } = bufferSize;
    public ConsumerGroupRetryStrategy RetryStrategy { get; } = retryStrategy;
}
