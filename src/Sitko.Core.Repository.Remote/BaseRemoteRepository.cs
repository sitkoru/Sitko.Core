using System.Linq.Expressions;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.FluentValidation.Graph;

namespace Sitko.Core.Repository.Remote;

public interface IRemoteRepository : IRepository
{
}

public class BaseRemoteRepository<TEntity, TEntityPk> : BaseRepository<TEntity, TEntityPk, RemoteRepositoryQuery<TEntity>>, IRemoteRepository where TEntity : class, IEntity<TEntityPk>
{

    private readonly IRemoteRepositoryTransport repositoryTransport;

    private List<RepositoryRecord<TEntity, TEntityPk>>? batch;

    protected BaseRemoteRepository(RemoteRepositoryContext<TEntity, TEntityPk> repositoryContext, IRemoteRepositoryTransport repositoryTransport) : base(repositoryContext)
    {
        this.repositoryTransport = repositoryTransport;
    }

    public override Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public override Task<bool> CommitTransactionAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public override Task<bool> RollbackTransactionAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    //go to server map model and update existing obj. Automapper?
    public override Task RefreshAsync(TEntity entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<RemoteRepositoryQuery<TEntity>> CreateRepositoryQueryAsync(
        CancellationToken cancellationToken = default)=>
        Task.FromResult(new RemoteRepositoryQuery<TEntity>());

    protected override async Task<(TEntity[] items, bool needCount)> DoGetAllAsync(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        var serialized = query.Serialize();

        //send it to server through remote transport service and recieve items
        var result = await repositoryTransport.GetAllAsync(serialized, cancellationToken);

        bool needCount = query.Offset != null || query.Limit != null;

        return (result.items, needCount);
    }

    protected override async Task<int> DoCountAsync(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        var serialized = query.Serialize();
        return await repositoryTransport.CountAsync(serialized, cancellationToken);
    }

    protected override async Task<int> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, int>> selector, CancellationToken cancellationToken = default)
    {
        var serialized = query.Serialize();
        return await repositoryTransport.SumAsync(serialized, cancellationToken);
    }

    protected override async Task<long> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, long>> selector, CancellationToken cancellationToken = default)
    {
        var serialized = query.Serialize();
        return await repositoryTransport.SumAsync(serialized, cancellationToken);
    }

    protected override Task<double> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, double>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<float> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, float>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<decimal> DoSumAsync(RemoteRepositoryQuery<TEntity> query, Expression<Func<TEntity, decimal>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<int?> DoSumAsync(RemoteRepositoryQuery<TEntity> query, Expression<Func<TEntity, int?>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<long?> DoSumAsync(RemoteRepositoryQuery<TEntity> query, Expression<Func<TEntity, long?>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<double?> DoSumAsync(RemoteRepositoryQuery<TEntity> query, Expression<Func<TEntity, double?>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<float?> DoSumAsync(RemoteRepositoryQuery<TEntity> query, Expression<Func<TEntity, float?>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<decimal?> DoSumAsync(RemoteRepositoryQuery<TEntity> query, Expression<Func<TEntity, decimal?>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override async Task<TEntity?> DoGetAsync(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        var serialized = query.Serialize();
        return await repositoryTransport.GetAsync(serialized, cancellationToken);
    }

    protected override Task DoSaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    protected override async Task<PropertyChange[]> GetChangesAsync(TEntity item) => throw new NotImplementedException();
    protected override async Task DoAddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await repositoryTransport.AddAsync<TEntity, TEntityPk>(entity, cancellationToken);
    }

    protected override async Task<PropertyChange[]> DoUpdateAsync(TEntity entity, TEntity? oldEntity,
        CancellationToken cancellationToken = default)
    {
        return await repositoryTransport.UpdateAsync(entity, oldEntity, cancellationToken);
    }

    protected override async Task DoDeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await repositoryTransport.DeleteAsync(entity, cancellationToken);
    }
}
