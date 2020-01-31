using System;
using System.Threading.Tasks;

namespace Sitko.Core.Repository
{
    public interface IRepository
    {
    }

    public interface IRepository<TEntity, TEntityPk> : IRepository where TEntity : class, IEntity<TEntityPk>
    {
        Task<(TEntity[] items, int itemsCount)> GetAllAsync();
        Task<(TEntity[] items, int itemsCount)> GetAllAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery);
        Task<(TEntity[] items, int itemsCount)> GetAllAsync(Action<IRepositoryQuery<TEntity>> configureQuery);

        Task<TEntity?> GetAsync();
        Task<TEntity?> GetAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery);
        Task<TEntity?> GetAsync(Action<IRepositoryQuery<TEntity>> configureQuery);
        
        Task<int> CountAsync();
        Task<int> CountAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery);
        Task<int> CountAsync(Action<IRepositoryQuery<TEntity>> configureQuery);

        Task<TEntity?> GetByIdAsync(TEntityPk id, Func<IRepositoryQuery<TEntity>, Task> configureQuery);
        Task<TEntity?> GetByIdAsync(TEntityPk id, Action<IRepositoryQuery<TEntity>> configureQuery);
        Task<TEntity?> GetByIdAsync(TEntityPk id);

        Task<TEntity> NewAsync();

        Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids, Func<IRepositoryQuery<TEntity>, Task> configureQuery);
        Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids, Action<IRepositoryQuery<TEntity>> configureQuery);
        Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids);

        Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddAsync(TEntity entity);

        Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync(TEntity entity);

        Task<bool> DeleteAsync(TEntityPk id);
        Task<bool> DeleteAsync(TEntity entity);
        PropertyChange[] GetChanges(TEntity entity, TEntity oldEntity);
    }
}
