namespace Sitko.Core.Search;

public record SearcherEntity<T>(T SearchModel, IReadOnlyDictionary<string, IReadOnlyCollection<string>> Highlight)
    where T : BaseSearchModel;
