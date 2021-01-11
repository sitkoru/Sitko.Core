using System.Threading;
using System.Threading.Tasks;

namespace Sitko.Core.Repository.Search
{
    public interface IRepositorySearchProvider
    {
        Task ReindexAsync(int batchSize, CancellationToken cancellationToken = default);
    }

    public interface IRepositorySearchProvider<TEntity> : IRepositorySearchProvider
        where TEntity : class, IEntity
    {
    }
}
