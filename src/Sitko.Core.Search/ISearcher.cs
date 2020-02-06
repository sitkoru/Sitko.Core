using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitko.Core.Search
{
    public interface ISearcher<T> where T: BaseSearchModel
    {
        Task<bool> AddOrUpdateAsync(string indexName, IEnumerable<T> searchModels);
        Task<bool> DeleteAsync(string indexName, IEnumerable<T> searchModels);
        Task<bool> DeleteAsync(string indexName);
        Task<long> CountAsync(string indexName, string term);
        Task<T[]> SearchAsync(string indexName, string term, int limit);
        Task InitAsync(string indexName);
    }
}
