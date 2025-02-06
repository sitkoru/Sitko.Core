using Sitko.Core.Kafka;

namespace Sitko.Core.Queue.Kafka;

internal static class KafkaQueueHelper
{
    public static string GetProducerName(Type producerType) =>
        $"Queue_{producerType.Name}";

    public static Dictionary<Type, KafkaConsumerAttribute> GetConsumersWithAttributes(List<Type> consumers)
    {
        var consumersWithAttributes = new Dictionary<Type, KafkaConsumerAttribute>();
        foreach (var consumer in consumers)
        {
            var attributes = KafkaQueueHelper.GetConsumerAttributes(consumer);
            if (attributes == null)
            {
                continue;
            }
            consumersWithAttributes.Add(consumer, attributes);
        }

        return consumersWithAttributes;
    }

    private static KafkaConsumerAttribute? GetConsumerAttributes(Type consumer, bool withInherit = true) =>
        consumer.GetCustomAttributes(typeof(KafkaConsumerAttribute), withInherit)
            .Cast<KafkaConsumerAttribute>().FirstOrDefault();
}
