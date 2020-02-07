using System;
using System.Threading.Tasks;

namespace Sitko.Core.Search
{
    public interface ISearchProvider
    {
        bool CanProcess(Type type);
        Task DeleteIndexAsync();
        Task<long> CountAsync(string term);
        Task InitAsync();
    }

    public interface ISearchProvider<T> : ISearchProvider where T : class
    {
        Task<T[]> SearchAsync(string term, int limit);
        Task<T[]> GetSimilarAsync(string id, int limit);
        Task AddOrUpdateEntityAsync(T entity);
        Task<bool> AddOrUpdateEntitiesAsync(T[] entities);
        Task<bool> DeleteEntityAsync(T entity);
        Task<bool> DeleteEntitiesAsync(T[] entities);
    }
}
