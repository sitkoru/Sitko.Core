namespace Sitko.Core.Queue.Kafka;

internal record EventMetadata(Type EventType, string EventTypeId, string PrefixedTopicName, string Topic);
