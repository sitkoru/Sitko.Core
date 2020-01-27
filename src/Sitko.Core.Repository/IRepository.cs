using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Sitko.Core.Repository
{
    public interface IRepository
    {
    }

    public interface IRepository<TEntity, TEntityPk> : IRepository where TEntity : class, IEntity<TEntityPk>
    {
        Task<(TEntity[] items, int itemsCount)> GetAllAsync();
        Task<(TEntity[] items, int itemsCount)> GetAllAsync(Func<RepositoryQuery<TEntity>, Task> configureQuery);
        Task<(TEntity[] items, int itemsCount)> GetAllAsync(Action<RepositoryQuery<TEntity>> configureQuery);

        Task<int> CountAsync();
        Task<int> CountAsync(Func<RepositoryQuery<TEntity>, Task> configureQuery);
        Task<int> CountAsync(Action<RepositoryQuery<TEntity>> configureQuery);

        Task<TEntity?> GetByIdAsync(TEntityPk id, Func<RepositoryQuery<TEntity>, Task> configureQuery);
        Task<TEntity?> GetByIdAsync(TEntityPk id, Action<RepositoryQuery<TEntity>> configureQuery);
        Task<TEntity?> GetByIdAsync(TEntityPk id);

        Task<TEntity> NewAsync();

        Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids, Func<RepositoryQuery<TEntity>, Task> configureQuery);
        Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids, Action<RepositoryQuery<TEntity>> configureQuery);
        Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids);

        Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddAsync(TEntity entity);

        Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync(TEntity entity);

        Task<bool> DeleteAsync(TEntityPk id);
        Task<bool> DeleteAsync(TEntity entity);
        PropertyChange[] GetChanges(TEntity entity, TEntity oldEntity);
        DbSet<T> Set<T>() where T : class;
    }
}
