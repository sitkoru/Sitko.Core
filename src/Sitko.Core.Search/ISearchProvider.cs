namespace Sitko.Core.Search;

public interface ISearchProvider
{
    bool CanProcess(Type type);
    Task DeleteIndexAsync(CancellationToken cancellationToken = default);
    Task<long> CountAsync(string term, CancellationToken cancellationToken = default);
    Task InitAsync(CancellationToken cancellationToken = default);
}

public interface ISearchProvider<TEntity, TEntityPk> : ISearchProvider where TEntity : class
{
    Task<TEntity[]> SearchAsync(string term, int limit, SearchType searchType, CancellationToken cancellationToken = default);
    Task<TEntityPk[]> GetIdsAsync(string term, int limit, SearchType searchType, CancellationToken cancellationToken = default);
    Task<TEntity[]> GetSimilarAsync(string id, int limit, CancellationToken cancellationToken = default);

    Task<TEntityPk[]> GetSimilarIdsAsync(string id, int limit,
        CancellationToken cancellationToken = default);

    Task AddOrUpdateEntityAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> AddOrUpdateEntitiesAsync(TEntity[] entities, CancellationToken cancellationToken = default);
    Task<bool> DeleteEntityAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteEntitiesAsync(TEntity[] entities, CancellationToken cancellationToken = default);
}

