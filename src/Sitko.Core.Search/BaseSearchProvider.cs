using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Search
{
    public abstract class BaseSearchProvider<T, TSearchModel> : ISearchProvider<T>
        where T : class where TSearchModel : BaseSearchModel
    {
        private readonly ISearcher<TSearchModel> _searcher;
        protected readonly ILogger<BaseSearchProvider<T, TSearchModel>> Logger;

        protected BaseSearchProvider(ILogger<BaseSearchProvider<T, TSearchModel>> logger,
            ISearcher<TSearchModel>? searcher = null)
        {
            _searcher = searcher ?? throw new Exception($"No searcher for provider {this}");
            Logger = logger;
        }

        private string IndexName => typeof(T).FullName!.ToLower().Replace(".", "_");

        public bool CanProcess(Type type)
        {
            return typeof(T) == type;
        }

        public Task DeleteIndexAsync(CancellationToken cancellationToken = default)
        {
            return _searcher.DeleteAsync(IndexName, cancellationToken);
        }

        public Task<long> CountAsync(string term, CancellationToken cancellationToken = default)
        {
            return _searcher.CountAsync(IndexName, term, cancellationToken);
        }

        public Task InitAsync(CancellationToken cancellationToken = default)
        {
            return _searcher.InitAsync(IndexName, cancellationToken);
        }

        public async Task<T[]> SearchAsync(string term, int limit, CancellationToken cancellationToken = default)
        {
            var result = await _searcher.SearchAsync(IndexName, term, limit, cancellationToken);
            return await LoadEntities(result, cancellationToken);
        }

        public async Task<string[]> SearchIdsAsync(string term, int limit,
            CancellationToken cancellationToken = default)
        {
            var result = await _searcher.SearchAsync(IndexName, term, limit, cancellationToken);
            return result.Select(m => m.Id).ToArray();
        }

        public async Task<T[]> GetSimilarAsync(string id, int limit, CancellationToken cancellationToken = default)
        {
            var result = await _searcher.GetSimilarAsync(IndexName, id, limit, cancellationToken);
            return await LoadEntities(result, cancellationToken);
        }

        protected virtual async Task<T[]> LoadEntities(TSearchModel[] searchModels,
            CancellationToken cancellationToken = default)
        {
            var entities = await GetEntitiesAsync(searchModels, cancellationToken);
            return entities.OrderBy(e => Array.FindIndex(searchModels, model => model.Id == GetId(e))).ToArray();
        }

        public Task AddOrUpdateEntityAsync(T entity, CancellationToken cancellationToken = default)
        {
            return AddOrUpdateEntitiesAsync(new[] {entity}, cancellationToken);
        }

        public async Task<bool> AddOrUpdateEntitiesAsync(T[] entities, CancellationToken cancellationToken = default)
        {
            return await _searcher.AddOrUpdateAsync(IndexName, await GetSearchModelsAsync(entities, cancellationToken),
                cancellationToken);
        }

        public async Task<bool> DeleteEntityAsync(T entity, CancellationToken cancellationToken = default)
        {
            return await _searcher.DeleteAsync(IndexName, await GetSearchModelsAsync(new[] {entity}, cancellationToken),
                cancellationToken);
        }

        public async Task<bool> DeleteEntitiesAsync(T[] entities, CancellationToken cancellationToken = default)
        {
            return await _searcher.DeleteAsync(IndexName, await GetSearchModelsAsync(entities, cancellationToken),
                cancellationToken);
        }

        protected abstract Task<TSearchModel[]> GetSearchModelsAsync(T[] entities,
            CancellationToken cancellationToken = default);

        protected abstract Task<T[]> GetEntitiesAsync(TSearchModel[] searchModels,
            CancellationToken cancellationToken = default);

        protected abstract string GetId(T entity);
    }
}
