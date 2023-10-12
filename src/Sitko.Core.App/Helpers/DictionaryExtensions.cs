namespace Sitko.Core.App.Helpers;

public static class DictionaryExtensions
{
    public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> factory)
    {
        if (!dict.TryGetValue(key, out var val))
        {
            val = factory();
            dict.Add(key, val);
        }

        return val;
    }
}
