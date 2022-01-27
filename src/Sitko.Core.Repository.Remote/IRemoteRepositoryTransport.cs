namespace Sitko.Core.Repository.Remote;

public interface IRemoteRepositoryTransport
{
     Task<T?> SendAsync<T>(CancellationToken cancellationToken = default);
     Task<int> DeleteAsync<TEntityPK>();
     Task<(TEntity[] items, int itemsCount)> GetAllAsync<TEntity>(SerializedQuery<TEntity> query, CancellationToken cancellationToken = default) where TEntity : class;
}
