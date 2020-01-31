using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sitko.Core.Repository
{
    public static class FieldsResolver
    {
        private static readonly ConcurrentDictionary<string, Dictionary<string, (string name, Type type)>> Properties =
            new ConcurrentDictionary<string, Dictionary<string, (string name, Type type)>>();

        public static (string name, Type type)? GetPropertyInfo<T>(string name)
        {
            var typeName = typeof(T).Name;
            Properties.GetOrAdd(typeName, typeof(T).GetProperties()
                .ToDictionary(p => p.Name.ToLowerInvariant(), p => (p.Name, p.PropertyType)));

            name = name.ToLowerInvariant();

            if (Properties[typeName].ContainsKey(name))
            {
                return Properties[typeName][name];
            }

            return null;
        }
    }
}
