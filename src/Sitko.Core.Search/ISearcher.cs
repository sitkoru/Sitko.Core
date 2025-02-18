namespace Sitko.Core.Search;

public interface ISearcher<T> where T : BaseSearchModel
{
    Task<bool> AddOrUpdateAsync(string indexName, IEnumerable<T> searchModels,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string indexName, IEnumerable<T> searchModels,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string indexName, CancellationToken cancellationToken = default);

    Task<long> CountAsync(string indexName, string term, SearchOptions? searchOptions,
        CancellationToken cancellationToken = default);

    Task<SearcherEntity<T>[]> SearchAsync(string indexName, string term, int limit,
        SearchOptions? searchOptions,
        CancellationToken cancellationToken = default);

    Task<SearcherEntity<T>[]> GetSimilarAsync(string indexName, string id, int limit,
        SearchOptions? searchOptions,
        CancellationToken cancellationToken = default);

    Task InitAsync(string indexName, CancellationToken cancellationToken = default);
}
