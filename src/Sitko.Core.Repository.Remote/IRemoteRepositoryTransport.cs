using System.Linq.Expressions;

namespace Sitko.Core.Repository.Remote;

public interface IRemoteRepositoryTransport
{
    Task<TEntity?> GetAsync<TEntity>(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class;

    Task<int> CountAsync<TEntity>(RemoteRepositoryQuery<TEntity> configureQuery,
        CancellationToken cancellationToken = default) where TEntity : class;

    Task<TReturn> SumAsync<TEntity, TReturn>(RemoteRepositoryQuery<TEntity> configureQuery, SumType type,
        CancellationToken cancellationToken = default) where TEntity : class where TReturn : struct;

    Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddAsync<TEntity, TEntityPk>(TEntity entity,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TEntityPk>;

    Task<AddOrUpdateOperationResult<TEntity, TEntityPk>[]> AddAsync<TEntity, TEntityPk>(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TEntityPk>;

    Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync<TEntity, TEntityPk>(TEntity entity, TEntity? oldEntity,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TEntityPk>;

    Task<bool> DeleteAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;

    Task<(TEntity[] items, int itemsCount)> GetAllAsync<TEntity>(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class;
}

public enum SumType
{
    Int = 1,
    Double = 2,
    Float = 3,
    Decimal = 4,
    Long = 5
}
