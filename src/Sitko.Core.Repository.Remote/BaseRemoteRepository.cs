using System.Linq.Expressions;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.FluentValidation.Graph;

namespace Sitko.Core.Repository.Remote;

public class BaseRemoteRepository<TEntity, TEntityPk, TQuery, TRepositoryOptions> : BaseRepository<TEntity, TEntityPk, TQuery> where TEntity : class, IEntity<TEntityPk>
    where TQuery : IRepositoryQuery<TEntity>
    where TRepositoryOptions : RemoteRepositoryOptions
{
    private readonly IRemoteRepositoryTransport repositoryTransport;
    private readonly IOptionsMonitor<TRepositoryOptions> optionsMonitor;

    private List<RepositoryRecord<TEntity, TEntityPk>>? batch;

    //Url problems for different controllers
    //1. conventional /digitclub/api/{TEntity} api - from Options

    protected BaseRemoteRepository(IRepositoryContext<TEntity, TEntityPk> repositoryContext, IRemoteRepositoryTransport repositoryTransport, IOptionsMonitor<TRepositoryOptions> optionsMonitor) : base(repositoryContext)
    {
        this.repositoryTransport = repositoryTransport;
        this.optionsMonitor = optionsMonitor;
        FiltersManager = repositoryContext.FiltersManager;
        FluentGraphValidator = repositoryContext.FluentGraphValidator;
        AccessCheckers = repositoryContext.AccessCheckers ?? new List<IAccessChecker<TEntity, TEntityPk>>();
        Logger = repositoryContext.Logger;
    }

    protected TRepositoryOptions Options => optionsMonitor.CurrentValue;
    [PublicAPI] protected FluentGraphValidator FluentGraphValidator { get; set; }

    [PublicAPI] protected RepositoryFiltersManager FiltersManager { get; }

    [PublicAPI] protected List<IAccessChecker<TEntity, TEntityPk>> AccessCheckers { get; }

    protected ILogger<IRepository<TEntity, TEntityPk>> Logger { get; }


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

    protected override Task<TQuery> CreateRepositoryQueryAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<(TEntity[] items, bool needCount)> DoGetAllAsync(TQuery query, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<int> DoCountAsync(TQuery query, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<int> DoSumAsync(TQuery query, Expression<Func<TEntity, int>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<long> DoSumAsync(TQuery query, Expression<Func<TEntity, long>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<double> DoSumAsync(TQuery query, Expression<Func<TEntity, double>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<float> DoSumAsync(TQuery query, Expression<Func<TEntity, float>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<decimal> DoSumAsync(TQuery query, Expression<Func<TEntity, decimal>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<int?> DoSumAsync(TQuery query, Expression<Func<TEntity, int?>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<long?> DoSumAsync(TQuery query, Expression<Func<TEntity, long?>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<double?> DoSumAsync(TQuery query, Expression<Func<TEntity, double?>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<float?> DoSumAsync(TQuery query, Expression<Func<TEntity, float?>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<decimal?> DoSumAsync(TQuery query, Expression<Func<TEntity, decimal?>> selector, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<TEntity?> DoGetAsync(TQuery query, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task DoSaveAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<PropertyChange[]> GetChangesAsync(TEntity item) => throw new NotImplementedException();

    protected override Task DoAddAsync(TEntity entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task<PropertyChange[]> DoUpdateAsync(TEntity entity, TEntity? oldEntity, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    protected override Task DoDeleteAsync(TEntity entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
