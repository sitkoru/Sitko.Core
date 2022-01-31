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

public abstract class BaseRemoteRepository<TEntity, TEntityPk> : BaseRepository<TEntity, TEntityPk, RemoteRepositoryQuery<TEntity>> where TEntity : class, IEntity<TEntityPk>
{

    private readonly IRemoteRepositoryTransport repositoryTransport;

    private List<RepositoryRecord<TEntity, TEntityPk>>? batch;

    //Url problems for different controllers
    //1. conventional /digitclub/api/{TEntity} api - from Options

    protected BaseRemoteRepository(IRepositoryContext<TEntity, TEntityPk> repositoryContext, IRemoteRepositoryTransport repositoryTransport) : base(repositoryContext)
    {
        this.repositoryTransport = repositoryTransport;
    }


    public override Task<(TEntity[] items, int itemsCount)> GetAllAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public override async Task<(TEntity[] items, int itemsCount)> GetAllAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        CancellationToken cancellationToken = default)
    {
        await CreateRepositoryQueryAsync(cancellationToken);

        //serializing query
        var query = new RemoteRepositoryQuery<TEntity>();
        await query.ConfigureAsync(configureQuery, cancellationToken);
        var serialized = query.Serialize();

        //send it to server through remote transport service and recieve items
        var result = await repositoryTransport.GetAllAsync(serialized, cancellationToken);

        await AfterLoadEntitiesAsync(result.items, cancellationToken);
        return result;
    }

    private async Task SaveAsync(RepositoryRecord<TEntity, TEntityPk> record,
        CancellationToken cancellationToken = default)
    {
        if (batch == null)
        {
            await DoSaveAsync(cancellationToken);
            await AfterSaveAsync(new[] { record }, cancellationToken);
        }
        else
        {
            batch.Add(record);
        }
    }

    private Task AfterLoadEntityAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var entities = new[] { entity };
        return AfterLoadAsync(entities, cancellationToken);
    }

    private Task AfterLoadEntitiesAsync(TEntity[] entities, CancellationToken cancellationToken = default) =>
        AfterLoadAsync(entities, cancellationToken);

    public override Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public override Task<bool> CommitTransactionAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public override Task<bool> RollbackTransactionAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public override Task RefreshAsync(TEntity entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task DoSaveAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<PropertyChange[]> GetChangesAsync(TEntity item) => throw new NotImplementedException();

    protected override Task DoAddAsync(TEntity entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<PropertyChange[]> DoUpdateAsync(TEntity entity, TEntity? oldEntity, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task DoDeleteAsync(TEntity entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
