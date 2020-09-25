using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sitko.Core.Repository
{
    public interface IRepository
    {
    }

    public interface IRepository<TEntity, TEntityPk> : IRepository where TEntity : class, IEntity<TEntityPk>
    {
        Task<(TEntity[] items, int itemsCount)> GetAllAsync(CancellationToken cancellationToken = default);

        Task<(TEntity[] items, int itemsCount)> GetAllAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
            CancellationToken cancellationToken = default);

        Task<(TEntity[] items, int itemsCount)> GetAllAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
            CancellationToken cancellationToken = default);

        Task<TEntity?> GetAsync(CancellationToken cancellationToken = default);

        Task<TEntity?> GetAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
            CancellationToken cancellationToken = default);

        Task<TEntity?> GetAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
            CancellationToken cancellationToken = default);

        Task<int> CountAsync(CancellationToken cancellationToken = default);

        Task<int> CountAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
            CancellationToken cancellationToken = default);

        Task<int> CountAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
            CancellationToken cancellationToken = default);

        Task<TEntity?> GetByIdAsync(TEntityPk id, Func<IRepositoryQuery<TEntity>, Task> configureQuery,
            CancellationToken cancellationToken = default);

        Task<TEntity?> GetByIdAsync(TEntityPk id, Action<IRepositoryQuery<TEntity>> configureQuery,
            CancellationToken cancellationToken = default);

        Task<TEntity?> GetByIdAsync(TEntityPk id, CancellationToken cancellationToken = default);

        Task<TEntity> NewAsync(CancellationToken cancellationToken = default);

        Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids, Func<IRepositoryQuery<TEntity>, Task> configureQuery,
            CancellationToken cancellationToken = default);

        Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids, Action<IRepositoryQuery<TEntity>> configureQuery,
            CancellationToken cancellationToken = default);

        Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids, CancellationToken cancellationToken = default);

        Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddAsync(TEntity entity,
            CancellationToken cancellationToken = default);

        Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync(TEntity entity,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(TEntityPk id, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
        PropertyChange[] GetChanges(TEntity entity, TEntity oldEntity);

        Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task<bool> CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task<bool> RollbackTransactionAsync(CancellationToken cancellationToken = default);

        Task<bool> BeginBatchAsync(CancellationToken cancellationToken = default);
        Task<bool> CommitBatchAsync(CancellationToken cancellationToken = default);
        Task<bool> RollbackBatchAsync(CancellationToken cancellationToken = default);
        
        Task RefreshAsync(TEntity entity, CancellationToken cancellationToken = default);
    }
}
