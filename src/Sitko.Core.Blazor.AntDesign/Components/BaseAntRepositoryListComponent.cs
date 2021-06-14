using System.Threading;
using System.Threading.Tasks;
using Sitko.Core.Repository;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public abstract class BaseAntRepositoryListComponent<TEntity, TEntityPk> : BaseAntListComponent<TEntity>
        where TEntity : class, IEntity<TEntityPk>, new()
    {
        protected IRepository<TEntity, TEntityPk> Repository => GetService<IRepository<TEntity, TEntityPk>>();

        protected override Task<(TEntity[] items, int itemsCount)> GetDataAsync(string orderBy,
            int page = 1, CancellationToken cancellationToken = default)
        {
            return Repository.GetAllAsync(query =>
            {
                ConfigureQuery(query);
                query.OrderByString(orderBy).Paginate(page, PageSize);
            }, cancellationToken);
        }

        protected virtual void ConfigureQuery(IRepositoryQuery<TEntity> query)
        {
        }
    }
}
