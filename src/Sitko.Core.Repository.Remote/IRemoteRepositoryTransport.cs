using System.Linq.Expressions;

namespace Sitko.Core.Repository.Remote;

public interface IRemoteRepositoryTransport
{
    Task<TEntity?> GetAsync<TEntity>(SerializedQuery<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class;

    Task<int> CountAsync<TEntity>(SerializedQuery<TEntity> configureQuery,
        CancellationToken cancellationToken = default) where TEntity : class;

    Task<int> SumAsync<TEntity>(SerializedQuery<TEntity> configureQuery, CancellationToken cancellationToken = default) where TEntity : class;

    Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddAsync<TEntity, TEntityPk>(TEntity entity,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TEntityPk>;

    Task<AddOrUpdateOperationResult<TEntity, TEntityPk>[]> AddAsync<TEntity, TEntityPk>(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TEntityPk>;

    Task<PropertyChange[]> UpdateAsync<TEntity>(TEntity entity, TEntity? oldEntity,
        CancellationToken cancellationToken = default) where TEntity : class;

    Task<bool> DeleteAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;

    Task<(TEntity[] items, int itemsCount)> GetAllAsync<TEntity>(SerializedQuery<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class;
}
