using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Sitko.Core.Repository;

namespace Sitko.Core.Blazor.AntDesignComponents.Components;

public class
    AntRepositoryList<TItem, TEntityPk> : BaseAntRepositoryList<TItem, TEntityPk, IRepository<TItem, TEntityPk>>
    where TItem : class, IEntity<TEntityPk>, new() where TEntityPk : notnull
{
}

public class
    BaseAntRepositoryList<TEntity, TEntityPk, TRepository> : BaseAntRepositoryListComponent<TEntity, TEntityPk,
        TRepository>
    where TEntity : class, IEntity<TEntityPk>, new()
    where TRepository : IRepository<TEntity, TEntityPk>
    where TEntityPk : notnull
{
}

public abstract class
    BaseAntRepositoryListComponent<TEntity, TEntityPk, TRepository> : BaseAntListComponent<TEntity>
    where TEntity : class, IEntity<TEntityPk>, new()
    where TRepository : IRepository<TEntity, TEntityPk>
    where TEntityPk : notnull
{
    [Parameter] public Func<IRepositoryQuery<TEntity>, Task>? ConfigureQuery { get; set; }

    protected Task<TResult> ExecuteRepositoryOperation<TResult>(
        Func<TRepository, Task<TResult>> operation) =>
        ExecuteServiceOperation(operation);

    protected override async Task<(TEntity[] items, int itemsCount)> GetDataAsync(LoadRequest<TEntity> request,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Load repository data");
        var result = await ExecuteRepositoryOperation(repository =>
        {
            Logger.LogDebug("Execute repository operation");
            var data = repository.GetAllAsync(async query =>
            {
                Logger.LogDebug("Execute DoConfigureQuery");
                await DoConfigureQuery(request, query);
                Logger.LogDebug("Apply sort");
                foreach (var sort in request.Sort)
                {
                    query = query.Order(sort.Operation);
                }

                Logger.LogDebug("Apply pagination");
                query.Paginate(request.Page, PageSize);
            }, cancellationToken);
            Logger.LogDebug("Repository operation is complete");
            return data;
        });
        Logger.LogDebug("Repository data loaded");
        return result;
    }

    private async Task DoConfigureQuery(LoadRequest<TEntity>? request, IRepositoryQuery<TEntity> query)
    {
        if (ConfigureQuery is not null)
        {
            Logger.LogDebug("Execute ConfigureQuery");
            await ConfigureQuery(query);
        }

        if (request is not null)
        {
            Logger.LogDebug("Apply request filters");
            foreach (var filter in request.Filters)
            {
                query = query.Where(filter.Operation);
            }
        }

        Logger.LogDebug("Execute ConfigureQueryAsync");
        await ConfigureQueryAsync(query);
        Logger.LogDebug("Query configured");
    }

    protected virtual Task ConfigureQueryAsync(IRepositoryQuery<TEntity> query) => Task.CompletedTask;

    public Task<int> SumAsync(Expression<Func<TEntity, int>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQuery(LastRequest, query);
            }, selector);
        });

    public Task<int?> SumAsync(Expression<Func<TEntity, int?>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQuery(LastRequest, query);
            }, selector);
        });

    public Task<long> SumAsync(Expression<Func<TEntity, long>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQuery(LastRequest, query);
            }, selector);
        });

    public Task<long?> SumAsync(Expression<Func<TEntity, long?>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQuery(LastRequest, query);
            }, selector);
        });

    public Task<double> SumAsync(Expression<Func<TEntity, double>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQuery(LastRequest, query);
            }, selector);
        });

    public Task<double?> SumAsync(Expression<Func<TEntity, double?>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQuery(LastRequest, query);
            }, selector);
        });

    public Task<float> SumAsync(Expression<Func<TEntity, float>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQuery(LastRequest, query);
            }, selector);
        });

    public Task<float?> SumAsync(Expression<Func<TEntity, float?>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQuery(LastRequest, query);
            }, selector);
        });

    public Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQuery(LastRequest, query);
            }, selector);
        });

    public Task<decimal?> SumAsync(Expression<Func<TEntity, decimal?>> selector) =>
        ExecuteRepositoryOperation(repository =>
        {
            return repository.SumAsync(async query =>
            {
                await DoConfigureQuery(LastRequest, query);
            }, selector);
        });
}

