using System;
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
            ISearcher<TSearchModel> searcher = null)
        {
            _searcher = searcher ?? throw new Exception($"No searcher for provider {this}");
            Logger = logger;
        }

        private string IndexName => typeof(T).FullName?.ToLower().Replace(".", "_");

        public bool CanProcess(Type type)
        {
            return typeof(T) == type;
        }

        public Task DeleteIndexAsync()
        {
            return _searcher.DeleteAsync(IndexName);
        }

        public Task<long> CountAsync(string term)
        {
            return _searcher.CountAsync(IndexName, term);
        }

        public Task InitAsync()
        {
            return _searcher.InitAsync(IndexName);
        }

        public async Task<T[]> SearchAsync(string term, int limit)
        {
            var result = await _searcher.SearchAsync(IndexName, term, limit);
            return await GetEntitiesAsync(result);
        }

        public async Task<T[]> GetSimilarAsync(string id, int limit)
        {
            var result = await _searcher.GetSimilarAsync(IndexName, id, limit);
            return await GetEntitiesAsync(result);
        }

        public Task AddOrUpdateEntityAsync(T entity)
        {
            return AddOrUpdateEntitiesAsync(new[] {entity});
        }

        public async Task<bool> AddOrUpdateEntitiesAsync(T[] entities)
        {
            return await _searcher.AddOrUpdateAsync(IndexName, await GetSearchModelsAsync(entities));
        }

        public async Task<bool> DeleteEntityAsync(T entity)
        {
            return await _searcher.DeleteAsync(IndexName, await GetSearchModelsAsync(new[] {entity}));
        }

        public async Task<bool> DeleteEntitiesAsync(T[] entities)
        {
            return await _searcher.DeleteAsync(IndexName, await GetSearchModelsAsync(entities));
        }

        protected abstract Task<TSearchModel[]> GetSearchModelsAsync(T[] entities);
        protected abstract Task<T[]> GetEntitiesAsync(TSearchModel[] searchModels);
    }
}
