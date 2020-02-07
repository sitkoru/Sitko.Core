using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Repository;

namespace Sitko.Core.Search
{
    public abstract class BaseSearchProvider<T> : ISearchProvider<T> where T : IEntity
    {
        private readonly ISearcher _searcher;
        protected readonly ILogger<BaseSearchProvider<T>> Logger;

        protected BaseSearchProvider(ILogger<BaseSearchProvider<T>> logger, ISearcher searcher = null)
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

        public async Task<T[]> GetSimilarAsync(T enity, int limit)
        {
            var result = await _searcher.GetSimilarAsync(IndexName, enity.GetId().ToString(), limit);
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

        protected abstract Task<SearchModel[]> GetSearchModelsAsync(T[] entities);
        protected abstract Task<T[]> GetEntitiesAsync(SearchModel[] searchModels);
    }
}
