namespace Sitko.Core.App.Collections;

public class EquatableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    where TKey : notnull
{
    public EquatableDictionary()
    {
    }

    public EquatableDictionary(Dictionary<TKey, TValue> dictionary) : base(dictionary)
    {
    }

#pragma warning disable CS0659
    public override bool Equals(object? obj)
#pragma warning restore CS0659
    {
        if (obj is EquatableDictionary<TKey, TValue> dictionary)
        {
            return this.OrderBy(pair => pair.Key).SequenceEqual(dictionary.OrderBy(pair => pair.Key));
        }

        // ReSharper disable once BaseObjectEqualsIsObjectEquals
        return base.Equals(obj);
    }
}

public static class EquatableDictionaryExtensions
{
    public static EquatableDictionary<TKey, TElement> ToEquatableDictionary<TSource, TKey, TElement>(
        this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        where TKey : notnull => new(source.ToDictionary(keySelector, elementSelector));
}
