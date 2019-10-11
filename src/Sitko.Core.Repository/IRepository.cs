using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sitko.Core.Repository
{
    public interface IRepository
    {
    }

    public interface IRepository<TEntity, TEntityPk> : IRepository where TEntity : class, IEntity<TEntityPk>
    {
        Task<(TEntity[] items, int itemsCount)> GetAllAsync(QueryContext<TEntity, TEntityPk> queryContext = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>> addConditionsCallback = null);

        Task<int> CountAsync(QueryContext<TEntity, TEntityPk> queryContext = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>> addConditionsCallback = null);

        Task<TEntity> GetByIdAsync(TEntityPk id, QueryContext<TEntity, TEntityPk> queryContext = null);

        Task<TEntity> NewAsync();

        Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids, QueryContext<TEntity, TEntityPk> queryContext = null);

        Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddAsync(TEntity entity);

        Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync(TEntity entity);

        Task<bool> DeleteAsync(TEntityPk id);
        Task<bool> DeleteAsync(TEntity entity);
        PropertyChange[] GetChanges(TEntity entity);
    }
}
