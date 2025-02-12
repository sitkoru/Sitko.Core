using System.Reflection;
using Sitko.Core.Queue.Kafka.Attributes;

namespace Sitko.Core.Queue.Kafka;

internal static class KafkaConfiguratorHelper
{
    public static TResult? FindAttribute<TResult>(this ICustomAttributeProvider provider, bool withInherit = true)
        where TResult : Attribute =>
        provider.GetCustomAttributes(typeof(TResult), withInherit).Cast<TResult>().FirstOrDefault();

    public static Dictionary<Type, MessageHandlerAttribute> GetConsumersWithAttributes(List<Type> consumers)
    {
        var consumersWithAttributes = new Dictionary<Type, MessageHandlerAttribute>();
        foreach (var consumer in consumers)
        {
            var attributes = consumer.FindAttribute<MessageHandlerAttribute>();
            if (attributes == null)
            {
                continue;
            }
            consumersWithAttributes.Add(consumer, attributes);
        }

        return consumersWithAttributes;
    }
}
