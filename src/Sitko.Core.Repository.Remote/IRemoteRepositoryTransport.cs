namespace Sitko.Core.Repository.Remote;

public interface IRemoteRepositoryTransport
{
    Task<TEntity?> GetAsync<TEntity>(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class;

    Task<int> CountAsync<TEntity>(RemoteRepositoryQuery<TEntity> configureQuery,
        CancellationToken cancellationToken = default) where TEntity : class;

    Task<TReturn> SumAsync<TEntity, TReturn>(RemoteRepositoryQuery<TEntity> configureQuery, SumType type,
        CancellationToken cancellationToken = default) where TEntity : class where TReturn : struct;

    Task<AddOrUpdateOperationResult<TEntity, TEntityPk>?> AddAsync<TEntity, TEntityPk>(TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TEntityPk> where TEntityPk : notnull;

    Task<AddOrUpdateOperationResult<TEntity, TEntityPk>[]?> AddAsync<TEntity, TEntityPk>(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TEntityPk> where TEntityPk : notnull;

    Task<AddOrUpdateOperationResult<TEntity, TEntityPk>?> UpdateAsync<TEntity, TEntityPk>(TEntity entity,
        TEntity? oldEntity,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity<TEntityPk> where TEntityPk : notnull;

    Task<bool> DeleteAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<(TEntity[] items, int itemsCount)> GetAllAsync<TEntity>(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default) where TEntity : class;
}

public enum SumType
{
    TypeInt = 1,
    TypeDouble = 2,
    TypeFloat = 3,
    TypeDecimal = 4,
    TypeLong = 5
}

