using System.Reflection;

namespace Sitko.Core.Queue.Kafka.Attributes;

public static class AttributeHelper
{
    public static TResult? FindAttribute<TResult>(this ICustomAttributeProvider provider, bool withInherit = true)
        where TResult : Attribute =>
        provider.GetCustomAttributes(typeof(TResult), withInherit).Cast<TResult>().FirstOrDefault();
}
