using System.Linq.Expressions;
using System.Text.Json;
using JetBrains.Annotations;
using KellermanSoftware.CompareNetObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App.Json;
using Sitko.FluentValidation.Graph;

namespace Sitko.Core.Repository.Remote;

public interface IRemoteRepository : IRepository
{
}

public class BaseRemoteRepository<TEntity, TEntityPk> : BaseRepository<TEntity, TEntityPk, RemoteRepositoryQuery<TEntity>>, IRemoteRepository where TEntity : class, IEntity<TEntityPk>
{

    private readonly IRemoteRepositoryTransport repositoryTransport;

    private bool isTransactionStarted;
    private CompareLogic? comparer;

    private List<RepositoryRecord<TEntity, TEntityPk>>? batch;
    private List<Func<Task>> TransactionActions { get; }
    private Dictionary<TEntityPk, TEntity> Snapshots { get; }

    protected BaseRemoteRepository(RemoteRepositoryContext<TEntity, TEntityPk> repositoryContext) : base(
        repositoryContext) =>
        repositoryTransport = repositoryContext.RepositoryTransport;

    public override async Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        isTransactionStarted = true;
        if (TransactionActions.Any())
        {
            throw new Exception();
        }

        return true;
    }

    public override async Task<bool> CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        //if not active = throw or something
        if (!isTransactionStarted)
        {
            throw new Exception();
        }
        //if active - do all from list and close
        foreach (var action in TransactionActions)
        {
            await action();
        }
        return true;
    }

    public override async Task<bool> RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        //if not active do nothing
        if (!isTransactionStarted)
        {
            return false;
        }
        //if active and actions not null - clear list and close
        if (TransactionActions.Any())
        {
            TransactionActions.Clear();
            isTransactionStarted = false;
        }
        return true;
    }

    public override async Task<TEntity> RefreshAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(entity.Id);
    }

    protected override Task<RemoteRepositoryQuery<TEntity>> CreateRepositoryQueryAsync(
        CancellationToken cancellationToken = default)=>
        Task.FromResult(new RemoteRepositoryQuery<TEntity>());

    protected override async Task<(TEntity[] items, int itemsCount, bool needCount)> DoGetAllAsync(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        //send it to server through remote transport service and recieve items
        var result = await repositoryTransport.GetAllAsync(query, cancellationToken);
        foreach (var item in result.items)
        {
            Snapshots.Add(item.Id, CreateEntitySnapshot(item));
        }
        return (result.items, result.itemsCount, false);
    }

    protected override async Task<int> DoCountAsync(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        return await repositoryTransport.CountAsync(query, cancellationToken);
    }

    protected override async Task<int> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, int>> selector, CancellationToken cancellationToken = default)
    {
        return (int)await repositoryTransport.SumAsync<TEntity, int>(query, cancellationToken);
    }

    protected override async Task<long> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, long>> selector, CancellationToken cancellationToken = default)
    {
        return (long)await repositoryTransport.SumAsync<TEntity, long>(query, cancellationToken);
    }

    protected override async Task<double> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, double>> selector, CancellationToken cancellationToken = default)
    {
        return (double)await repositoryTransport.SumAsync<TEntity, double>(query, cancellationToken);
    }

    protected override async Task<float> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, float>> selector, CancellationToken cancellationToken = default)
    {
        return (float)await repositoryTransport.SumAsync<TEntity, float>(query, cancellationToken);
    }

    protected override async Task<decimal> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, decimal>> selector, CancellationToken cancellationToken = default)
    {
        return (decimal)await repositoryTransport.SumAsync<TEntity, decimal>(query, cancellationToken);
    }

    protected override async Task<int?> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, int?>> selector, CancellationToken cancellationToken = default)
    {
        return await repositoryTransport.SumAsync<TEntity, int>(query, cancellationToken);
    }

    protected override async Task<long?> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, long?>> selector, CancellationToken cancellationToken = default)
    {
        return await repositoryTransport.SumAsync<TEntity, long>(query, cancellationToken);
    }

    protected override async Task<double?> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, double?>> selector, CancellationToken cancellationToken = default)
    {
        return await repositoryTransport.SumAsync<TEntity, double>(query, cancellationToken);
    }

    protected override async Task<float?> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, float?>> selector, CancellationToken cancellationToken = default)
    {
        return await repositoryTransport.SumAsync<TEntity, float>(query, cancellationToken);
    }

    protected override async Task<decimal?> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, decimal?>> selector, CancellationToken cancellationToken = default)
    {
        return await repositoryTransport.SumAsync<TEntity, decimal>(query, cancellationToken);
    }

    protected override async Task<TEntity?> DoGetAsync(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        var result = await repositoryTransport.GetAsync(query, cancellationToken);
        Snapshots.Add(result.Id, CreateEntitySnapshot(result));
        return result;
    }

    protected override Task DoSaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private CompareLogic GetComparer()
    {
        if (comparer is null)
        {
            var comparerOptions =
                new ComparisonConfig { MaxDifferences = 100, Caching = true, AutoClearCache = true };
            ConfigureComparer(comparerOptions);
            comparer = new CompareLogic(comparerOptions);
        }

        return comparer;
    }
    protected virtual void ConfigureComparer(ComparisonConfig comparisonConfig)
    {
    }
    protected override async Task<PropertyChange[]> GetChangesAsync(TEntity item)
    {
        var changes = new List<PropertyChange>();
        if (Snapshots[item.Id] is not null)
        {
            var differences = GetComparer().Compare(Snapshots[item.Id], CreateEntitySnapshot(item));
            if (!differences.AreEqual)
            {
                foreach (var difference in differences.Differences)
                {
                    var change = new PropertyChange(difference.GetShortItem(),difference.Object1Value,difference.Object2Value, ChangeType.Modified);
                    changes.Add(change);
                }
            }
        }

        return changes.ToArray();
    }
    protected virtual TEntity? CreateEntitySnapshot(TEntity? entity) => JsonHelper.Clone(entity);
    protected override async Task DoAddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Snapshots.Add(entity.Id, CreateEntitySnapshot(entity));
        if (isTransactionStarted)
        {
            TransactionActions.Add(()=>repositoryTransport.AddAsync<TEntity, TEntityPk>(entity, cancellationToken));
        }
        else
        {
            await repositoryTransport.AddAsync<TEntity, TEntityPk>(entity, cancellationToken);
        }
    }

    protected override async Task<PropertyChange[]> DoUpdateAsync(TEntity entity, TEntity? oldEntity,
        CancellationToken cancellationToken = default)
    {
        Snapshots.Add(entity.Id, CreateEntitySnapshot(entity));
        var changes = await GetChangesAsync(entity);
        if (isTransactionStarted)
        {
            TransactionActions.Add(()=>repositoryTransport.UpdateAsync<TEntity, TEntityPk>(entity, oldEntity, cancellationToken));
        }
        else
        {
            await repositoryTransport.UpdateAsync<TEntity, TEntityPk>(entity, oldEntity, cancellationToken);
        }
        return changes;
    }

    protected override async Task DoDeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (isTransactionStarted)
        {
            TransactionActions.Add(()=>repositoryTransport.DeleteAsync(entity, cancellationToken));
        }
        else
        {
            await repositoryTransport.DeleteAsync(entity, cancellationToken);
        }

        if (Snapshots[entity.Id] is not null)
        {
            Snapshots.Remove(entity.Id);
        }
    }
}
