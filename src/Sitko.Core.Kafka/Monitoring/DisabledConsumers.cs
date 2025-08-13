using System.Collections.Concurrent;
using KafkaFlow.Consumers;

namespace Sitko.Core.Kafka.Monitoring;

public static class DisabledConsumers
{
    private static readonly ConcurrentDictionary<string, bool> disabledConsumers = new();

    public static void Add(IMessageConsumer consumer) =>
        disabledConsumers[GetConsumerKey(consumer)] = true;

    public static void Remove(IMessageConsumer consumer) =>
        disabledConsumers.TryRemove(GetConsumerKey(consumer), out _);

    public static bool IsDisabled(IMessageConsumer consumer) =>
        disabledConsumers.ContainsKey(GetConsumerKey(consumer));

    private static string GetConsumerKey(IMessageConsumer consumer) =>
        $"{consumer.ClusterName}|{consumer.ConsumerName}";
}
