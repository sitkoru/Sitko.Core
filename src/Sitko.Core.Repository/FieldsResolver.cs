using System.Collections.Concurrent;

namespace Sitko.Core.Repository;

public static class FieldsResolver
{
    private static readonly ConcurrentDictionary<string, Dictionary<string, (string name, Type type)>> Properties =
        new();

    public static (string name, Type type)? GetPropertyInfo<T>(string name)
    {
        var typeName = typeof(T).Name;
        Properties.GetOrAdd(typeName, typeof(T).GetProperties()
            .ToDictionary(p => p.Name.ToLowerInvariant(), p => (p.Name, p.PropertyType)));

        name = name.ToLowerInvariant();

        if (Properties[typeName].TryGetValue(name, out var value))
        {
            return value;
        }

        return null;
    }
}

