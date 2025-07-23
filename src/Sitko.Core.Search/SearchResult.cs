namespace Sitko.Core.Search;

public record SearchResult<TEntity>(TEntity Entity, IReadOnlyDictionary<string, IReadOnlyCollection<string>> Highlight);
