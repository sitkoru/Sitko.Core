namespace Sitko.Core.Queue.Kafka;

public enum ConsumerGroupRetryStrategy
{
    None = 0,
    Simple = 1,
    Forever = 2,
    DurableOrdered = 3,
    DurableLatest = 4
}
