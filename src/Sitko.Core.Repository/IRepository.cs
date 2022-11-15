using System.Linq.Expressions;
using JetBrains.Annotations;

namespace Sitko.Core.Repository;

[PublicAPI]
public interface IRepository
{
}

[PublicAPI]
public interface IRepository<TEntity, TEntityPk> : IRepository
    where TEntity : class, IEntity<TEntityPk> where TEntityPk : notnull
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

    Task<int> SumAsync(Expression<Func<TEntity, int>> selector, CancellationToken cancellationToken = default);

    Task<int> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, int>> selector,
        CancellationToken cancellationToken = default);

    Task<int> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, int>> selector,
        CancellationToken cancellationToken = default);

    Task<long> SumAsync(Expression<Func<TEntity, long>> selector, CancellationToken cancellationToken = default);

    Task<long> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, long>> selector,
        CancellationToken cancellationToken = default);

    Task<long> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, long>> selector,
        CancellationToken cancellationToken = default);

    Task<double> SumAsync(Expression<Func<TEntity, double>> selector,
        CancellationToken cancellationToken = default);

    Task<double> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, double>> selector,
        CancellationToken cancellationToken = default);

    Task<double> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, double>> selector,
        CancellationToken cancellationToken = default);

    Task<float> SumAsync(Expression<Func<TEntity, float>> selector, CancellationToken cancellationToken = default);

    Task<float> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, float>> selector,
        CancellationToken cancellationToken = default);

    Task<float> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, float>> selector,
        CancellationToken cancellationToken = default);

    Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector,
        CancellationToken cancellationToken = default);

    Task<decimal> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, decimal>> selector,
        CancellationToken cancellationToken = default);

    Task<decimal> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, decimal>> selector,
        CancellationToken cancellationToken = default);

    Task<int?> SumAsync(Expression<Func<TEntity, int?>> selector, CancellationToken cancellationToken = default);

    Task<int?> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, int?>> selector,
        CancellationToken cancellationToken = default);

    Task<int?> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, int?>> selector,
        CancellationToken cancellationToken = default);

    Task<long?> SumAsync(Expression<Func<TEntity, long?>> selector, CancellationToken cancellationToken = default);

    Task<long?> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, long?>> selector,
        CancellationToken cancellationToken = default);

    Task<long?> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, long?>> selector,
        CancellationToken cancellationToken = default);

    Task<double?> SumAsync(Expression<Func<TEntity, double?>> selector,
        CancellationToken cancellationToken = default);

    Task<double?> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, double?>> selector,
        CancellationToken cancellationToken = default);

    Task<double?> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, double?>> selector,
        CancellationToken cancellationToken = default);

    Task<float?> SumAsync(Expression<Func<TEntity, float?>> selector,
        CancellationToken cancellationToken = default);

    Task<float?> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, float?>> selector,
        CancellationToken cancellationToken = default);

    Task<float?> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, float?>> selector,
        CancellationToken cancellationToken = default);

    Task<decimal?> SumAsync(Expression<Func<TEntity, decimal?>> selector,
        CancellationToken cancellationToken = default);

    Task<decimal?> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, decimal?>> selector,
        CancellationToken cancellationToken = default);

    Task<decimal?> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, decimal?>> selector,
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

    Task<AddOrUpdateOperationResult<TEntity, TEntityPk>[]> AddAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync(TEntity entity,
        CancellationToken cancellationToken = default);

    Task<AddOrUpdateOperationResult<TEntity, TEntityPk>[]> UpdateAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    Task<AddOrUpdateOperationResult<TEntity, TEntityPk>[]> UpdateAsync(
        IEnumerable<(TEntity entity, TEntity? oldEntity)> entities,
        CancellationToken cancellationToken = default);

    Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync(TEntity entity, TEntity? oldEntity,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(TEntityPk id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(IEnumerable<TEntityPk> ids, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<bool> CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task<bool> RollbackTransactionAsync(CancellationToken cancellationToken = default);

    Task<bool> BeginBatchAsync(CancellationToken cancellationToken = default);
    Task<bool> CommitBatchAsync(CancellationToken cancellationToken = default);
    Task<bool> RollbackBatchAsync(CancellationToken cancellationToken = default);

    Task<TEntity> RefreshAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> HasChangesAsync(TEntity entity);
    TEntity CreateSnapshot(TEntity entity);
}

