using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Search;

[PublicAPI]
public abstract class BaseSearchProvider<T, TEntityPk, TSearchModel> : ISearchProvider<T, TEntityPk, TSearchModel>
    where T : class where TSearchModel : BaseSearchModel
{
    private readonly ISearcher<TSearchModel> searcher;

    protected BaseSearchProvider(ILogger<BaseSearchProvider<T, TEntityPk, TSearchModel>> logger,
        ISearcher<TSearchModel>? searcher = null)
    {
        this.searcher = searcher ?? throw new InvalidOperationException($"No searcher for provider {this}");
        Logger = logger;
    }

    protected ILogger<BaseSearchProvider<T, TEntityPk, TSearchModel>> Logger { get; }

    private static string IndexName => typeof(T).FullName!.ToLowerInvariant().Replace(".", "_");

    public bool CanProcess(Type type) => typeof(T) == type;

    public Task DeleteIndexAsync(CancellationToken cancellationToken = default) =>
        searcher.DeleteAsync(IndexName, cancellationToken);

    public Task<long> CountAsync(string term, SearchOptions? searchOptions = null,
        CancellationToken cancellationToken = default) =>
        searcher.CountAsync(IndexName, term, searchOptions, cancellationToken);

    public Task InitAsync(CancellationToken cancellationToken = default) =>
        searcher.InitAsync(IndexName, cancellationToken);

    public async Task<SearchResult<T>[]> SearchAsync(string term, int limit,
        SearchOptions? searchOptions = null, CancellationToken cancellationToken = default)
    {
        var result =
            await searcher.SearchAsync(IndexName, term, limit, searchOptions, cancellationToken);
        return await LoadEntities(result, cancellationToken);
    }

    public async Task<TEntityPk[]> GetIdsAsync(string term, int limit, SearchOptions? searchOptions = null,
        CancellationToken cancellationToken = default)
    {
        var result =
            await searcher.SearchAsync(IndexName, term, limit, searchOptions, cancellationToken);
        return result.Select(m => ParseId(m.SearchModel.Id)).ToArray();
    }

    public async Task<SearchResult<T>[]> GetSimilarAsync(string id, int limit,
        SearchOptions? searchOptions = null,
        CancellationToken cancellationToken = default)
    {
        var result = await searcher.GetSimilarAsync(IndexName, id, limit, searchOptions, cancellationToken);
        return await LoadEntities(result, cancellationToken);
    }

    public async Task<TEntityPk[]> GetSimilarIdsAsync(string id, int limit, SearchOptions? searchOptions = null,
        CancellationToken cancellationToken = default)
    {
        var result = await searcher.GetSimilarAsync(IndexName, id, limit, searchOptions, cancellationToken);
        return result.Select(m => ParseId(m.SearchModel.Id)).ToArray();
    }

    public Task AddOrUpdateEntityAsync(T entity, CancellationToken cancellationToken = default) =>
        AddOrUpdateEntitiesAsync([entity], cancellationToken);

    public async Task<bool> AddOrUpdateEntitiesAsync(T[] entities, CancellationToken cancellationToken = default) =>
        await searcher.AddOrUpdateAsync(IndexName, await GetSearchModelsAsync(entities, cancellationToken),
            cancellationToken);

    public async Task<bool> DeleteEntityAsync(T entity, CancellationToken cancellationToken = default) =>
        await searcher.DeleteAsync(IndexName, await GetSearchModelsAsync(new[] { entity }, cancellationToken),
            cancellationToken);

    public async Task<bool> DeleteEntitiesAsync(T[] entities, CancellationToken cancellationToken = default) =>
        await searcher.DeleteAsync(IndexName, await GetSearchModelsAsync(entities, cancellationToken),
            cancellationToken);

    protected abstract TEntityPk ParseId(string id);

    protected virtual async Task<SearchResult<T>[]> LoadEntities(SearcherEntity<TSearchModel>[] searchModels,
        CancellationToken cancellationToken = default)
    {
        var entities = await GetEntitiesAsync(searchModels, cancellationToken);
        return entities.OrderBy(e => Array.FindIndex(searchModels, model => model.SearchModel.Id == GetId(e.Entity))).ToArray();
    }

    protected abstract Task<TSearchModel[]> GetSearchModelsAsync(T[] entities,
        CancellationToken cancellationToken = default);

    protected abstract Task<SearchResult<T>[]> GetEntitiesAsync(SearcherEntity<TSearchModel>[] searchModels,
        CancellationToken cancellationToken = default);

    protected abstract string GetId(T entity);
}
