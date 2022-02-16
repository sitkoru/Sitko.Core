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
    private readonly Dictionary<TEntityPk, TEntity> snapshots = new();

    private readonly List<Func<Task>> transactionActions = new();

    private CompareLogic? comparer;

    private bool isTransactionStarted;

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
            snapshots[item.Id] = CreateEntitySnapshot(item);
        }

        return (result.items, result.itemsCount, false);
    }

    protected override async Task<int> DoCountAsync(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        return await repositoryTransport.CountAsync(query, cancellationToken);
    }

    protected override async Task<int> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, int>> selector, CancellationToken cancellationToken = default) =>
        await repositoryTransport.SumAsync<TEntity, int>(query.Select(selector), SumType.Int, cancellationToken);

    protected override async Task<long> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, long>> selector, CancellationToken cancellationToken = default) =>
        await repositoryTransport.SumAsync<TEntity, long>(query.Select(selector), SumType.Long, cancellationToken);

    protected override async Task<double> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, double>> selector, CancellationToken cancellationToken = default) =>
        await repositoryTransport.SumAsync<TEntity, double>(query.Select(selector), SumType.Double, cancellationToken);

    protected override async Task<float> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, float>> selector, CancellationToken cancellationToken = default) =>
        await repositoryTransport.SumAsync<TEntity, float>(query.Select(selector), SumType.Float, cancellationToken);

    protected override async Task<decimal> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, decimal>> selector, CancellationToken cancellationToken = default) =>
        await repositoryTransport.SumAsync<TEntity, decimal>(query.Select(selector), SumType.Decimal,
            cancellationToken);

    protected override async Task<int?> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, int?>> selector, CancellationToken cancellationToken = default) =>
        await repositoryTransport.SumAsync<TEntity, int>(query.Select(selector), SumType.Int, cancellationToken);

    protected override async Task<long?> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, long?>> selector, CancellationToken cancellationToken = default) =>
        await repositoryTransport.SumAsync<TEntity, long>(query.Select(selector), SumType.Long, cancellationToken);

    protected override async Task<double?> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, double?>> selector, CancellationToken cancellationToken = default) =>
        await repositoryTransport.SumAsync<TEntity, double>(query.Select(selector), SumType.Double, cancellationToken);

    protected override async Task<float?> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, float?>> selector, CancellationToken cancellationToken = default) =>
        await repositoryTransport.SumAsync<TEntity, float>(query.Select(selector), SumType.Float, cancellationToken);

    protected override async Task<decimal?> DoSumAsync(RemoteRepositoryQuery<TEntity> query,
        Expression<Func<TEntity, decimal?>> selector, CancellationToken cancellationToken = default) =>
        await repositoryTransport.SumAsync<TEntity, decimal>(query.Select(selector), SumType.Decimal,
            cancellationToken);

    protected override async Task<TEntity?> DoGetAsync(RemoteRepositoryQuery<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        var result = await repositoryTransport.GetAsync(query, cancellationToken);
        if (result is not null)
        {
            snapshots[result.Id] = CreateEntitySnapshot(result);
        }

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
        if (snapshots.ContainsKey(item.Id))
        {
            var differences = GetComparer().Compare(snapshots[item.Id], CreateEntitySnapshot(item));
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
            transactionActions.Add(async () =>
            {
                var result = await repositoryTransport.AddAsync<TEntity, TEntityPk>(entity, cancellationToken);
                if (result.IsSuccess)
                {
                    snapshots[entity.Id] = CreateEntitySnapshot(result.Entity);
                }
            });
        }
        else
        {
            var result = await repositoryTransport.AddAsync<TEntity, TEntityPk>(entity, cancellationToken);
            if (result.IsSuccess)
            {
                snapshots[entity.Id] = CreateEntitySnapshot(result.Entity);
            }
        }
    }

    protected override async Task<PropertyChange[]> DoUpdateAsync(TEntity entity, TEntity? oldEntity,
        CancellationToken cancellationToken = default)
    {
        Snapshots.Add(entity.Id, CreateEntitySnapshot(entity));
        var changes = await GetChangesAsync(entity);
        if (isTransactionStarted)
        {
            transactionActions.Add(async () =>
            {
                var result =
                    await repositoryTransport.UpdateAsync<TEntity, TEntityPk>(entity, oldEntity, cancellationToken);
                if (result.IsSuccess)
                {
                    snapshots.Add(entity.Id, CreateEntitySnapshot(result.Entity));
                }
            });
        }
        else
        {
            var result =
                await repositoryTransport.UpdateAsync<TEntity, TEntityPk>(entity, oldEntity, cancellationToken);
            if (result.IsSuccess)
            {
                snapshots[entity.Id] = CreateEntitySnapshot(result.Entity);
            }
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

        if (snapshots.ContainsKey(entity.Id))
        {
            snapshots.Remove(entity.Id);
        }
    }
}
