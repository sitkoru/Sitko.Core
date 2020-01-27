using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitko.Core.Search
{
    public interface ISearcher
    {
        Task<bool> AddOrUpdateAsync(string indexName, IEnumerable<SearchModel> searchModels);
        Task<bool> DeleteAsync(string indexName, IEnumerable<SearchModel> searchModels);
        Task<bool> DeleteAsync(string indexName);
        Task<long> CountAsync(string indexName, string term);
        Task<SearchModel[]> SearchAsync(string indexName, string term, int limit);
        Task InitAsync(string indexName);
    }
}
