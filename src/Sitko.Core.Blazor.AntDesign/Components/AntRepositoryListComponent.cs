using System.Threading.Tasks;
using Sitko.Core.Repository;

namespace Sitko.Core.Blazor.AntDesign.Components
{
    public abstract class AntRepositoryListComponent<TEntity, TEntityPk> : AntListComponent<TEntity>
        where TEntity : class, IEntity<TEntityPk>, new()
    {
        protected IRepository<TEntity, TEntityPk> Repository => GetService<IRepository<TEntity, TEntityPk>>();

        protected override Task<(TEntity[] items, int itemsCount)> GetDataAsync(string orderBy,
            int page = 1)
        {
            return Repository.GetAllAsync(query =>
                {
                    ConfigureQuery(query);
                    query.OrderByString(orderBy).Paginate(page, PageSize);
                }
            );
        }

        protected virtual void ConfigureQuery(IRepositoryQuery<TEntity> query)
        {
        }
    }
}
