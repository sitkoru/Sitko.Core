namespace Sitko.Core.Search;

public interface ISearchProvider
{
    bool CanProcess(Type type);
    Task DeleteIndexAsync(CancellationToken cancellationToken = default);

    Task<long> CountAsync(string term, SearchOptions? searchOptions = default,
        CancellationToken cancellationToken = default);

    Task InitAsync(CancellationToken cancellationToken = default);
}

public interface ISearchProvider<TEntity> : ISearchProvider where TEntity : class
{
    Task AddOrUpdateEntityAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> AddOrUpdateEntitiesAsync(TEntity[] entities, CancellationToken cancellationToken = default);
    Task<bool> DeleteEntityAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteEntitiesAsync(TEntity[] entities, CancellationToken cancellationToken = default);
}

public interface ISearchProvider<TEntity, TEntityPk> : ISearchProvider<TEntity> where TEntity : class
{
    Task<TEntity[]> SearchAsync(string term, int limit, SearchOptions? searchOptions = null,
        CancellationToken cancellationToken = default);

    Task<TEntityPk[]> GetIdsAsync(string term, int limit, SearchOptions? searchOptions = null,
        CancellationToken cancellationToken = default);

    Task<TEntity[]> GetSimilarAsync(string id, int limit, SearchOptions? searchOptions = null,
        CancellationToken cancellationToken = default);

    Task<TEntityPk[]> GetSimilarIdsAsync(string id, int limit, SearchOptions? searchOptions = null,
        CancellationToken cancellationToken = default);
}

public interface ISearchProvider<TEntity, TEntityPk, TSearchModel> : ISearchProvider<TEntity>
    where TEntity : class where TSearchModel : BaseSearchModel
{
    Task<SearchResult<TEntity>[]> SearchAsync(string term, int limit, SearchOptions? searchOptions = null,
        CancellationToken cancellationToken = default);

    Task<TEntityPk[]> GetIdsAsync(string term, int limit, SearchOptions? searchOptions = null,
        CancellationToken cancellationToken = default);

    Task<SearchResult<TEntity>[]> GetSimilarAsync(string id, int limit,
        SearchOptions? searchOptions = null,
        CancellationToken cancellationToken = default);

    Task<TEntityPk[]> GetSimilarIdsAsync(string id, int limit, SearchOptions? searchOptions = null,
        CancellationToken cancellationToken = default);
}
