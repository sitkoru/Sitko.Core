using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sitko.Core.Search
{
    public interface ISearchProvider
    {
        bool CanProcess(Type type);
        Task DeleteIndexAsync(CancellationToken cancellationToken = default);
        Task<long> CountAsync(string term, CancellationToken cancellationToken = default);
        Task InitAsync(CancellationToken cancellationToken = default);
    }

    public interface ISearchProvider<T> : ISearchProvider where T : class
    {
        Task<T[]> SearchAsync(string term, int limit, CancellationToken cancellationToken = default);
        Task<T[]> GetSimilarAsync(string id, int limit, CancellationToken cancellationToken = default);
        Task AddOrUpdateEntityAsync(T entity, CancellationToken cancellationToken = default);
        Task<bool> AddOrUpdateEntitiesAsync(T[] entities, CancellationToken cancellationToken = default);
        Task<bool> DeleteEntityAsync(T entity, CancellationToken cancellationToken = default);
        Task<bool> DeleteEntitiesAsync(T[] entities, CancellationToken cancellationToken = default);
    }
}
