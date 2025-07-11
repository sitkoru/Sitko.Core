namespace Sitko.Core.Queue.Kafka;

internal record ConsumerRegistration(
    Type EventHandler,
    Type EventType,
    string PrefixedTopicName,
    string TopicName,
    string PrefixedGroupName,
    string GroupName,
    int ParallelThreadCount,
    int BufferSize,
    ConsumerGroupRetryStrategy RetryStrategy);
