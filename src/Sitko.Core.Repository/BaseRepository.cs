using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Results;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Sitko.Core.App.Json;
using Sitko.FluentValidation.Graph;

namespace Sitko.Core.Repository;

public interface IRepositoryContext<TEntity, TEntityPk> where TEntity : class, IEntity<TEntityPk>
{
    RepositoryFiltersManager FiltersManager { get; }
    FluentGraphValidator FluentGraphValidator { get; }
    List<IAccessChecker<TEntity, TEntityPk>>? AccessCheckers { get; }
    ILogger<IRepository<TEntity, TEntityPk>> Logger { get; }
}

public abstract class BaseRepository<TEntity, TEntityPk, TQuery> : IRepository<TEntity, TEntityPk>
    where TEntity : class, IEntity<TEntityPk> where TQuery : IRepositoryQuery<TEntity>
{
    private List<RepositoryRecord<TEntity, TEntityPk>>? batch;

    protected BaseRepository(IRepositoryContext<TEntity, TEntityPk> repositoryContext)
    {
        FiltersManager = repositoryContext.FiltersManager;
        FluentGraphValidator = repositoryContext.FluentGraphValidator;
        AccessCheckers = repositoryContext.AccessCheckers ?? new List<IAccessChecker<TEntity, TEntityPk>>();
        Logger = repositoryContext.Logger;
    }

    [PublicAPI] protected FluentGraphValidator FluentGraphValidator { get; set; }

    [PublicAPI] protected RepositoryFiltersManager FiltersManager { get; }

    [PublicAPI] protected List<IAccessChecker<TEntity, TEntityPk>> AccessCheckers { get; }

    protected ILogger<IRepository<TEntity, TEntityPk>> Logger { get; }

    public abstract Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default);

    public abstract Task<bool> CommitTransactionAsync(CancellationToken cancellationToken = default);
    public abstract Task<bool> RollbackTransactionAsync(CancellationToken cancellationToken = default);

    public abstract Task<TEntity> RefreshAsync(TEntity entity, CancellationToken cancellationToken = default);

    public async Task<bool> HasChangesAsync(TEntity entity)
    {
        var changesResult = await GetChangesAsync(entity);
        return changesResult.Length > 0;
    }

    public virtual TEntity CreateSnapshot(TEntity entity) => JsonHelper.Clone(entity)!;


    public virtual Task<bool> BeginBatchAsync(CancellationToken cancellationToken = default)
    {
        if (batch != null)
        {
            return Task.FromResult(false);
        }

        batch = new List<RepositoryRecord<TEntity, TEntityPk>>();

        return Task.FromResult(true);
    }

    public virtual async Task<bool> CommitBatchAsync(CancellationToken cancellationToken = default)
    {
        if (batch == null)
        {
            return false;
        }

        await DoSaveAsync(cancellationToken);
        await AfterSaveAsync(batch, cancellationToken);

        batch = null;
        return true;
    }

    public virtual Task<bool> RollbackBatchAsync(CancellationToken cancellationToken = default)
    {
        if (batch == null)
        {
            return Task.FromResult(false);
        }

        batch = null;
        return Task.FromResult(true);
    }

    public virtual async Task<TEntity> NewAsync(CancellationToken cancellationToken = default)
    {
        var item = Activator.CreateInstance<TEntity>();
        await AfterLoadAsync(item, cancellationToken);
        return item;
    }

    public virtual async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddAsync(TEntity item,
        CancellationToken cancellationToken = default)
    {
        var result = await AddAsync(new[] { item }, cancellationToken);
        return result.First();
    }

    public virtual async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>[]> AddAsync(IEnumerable<TEntity> items,
        CancellationToken cancellationToken = default)
    {
        var results = new List<AddOrUpdateOperationResult<TEntity, TEntityPk>>();
        await BeginBatchAsync(cancellationToken);
        var hasErrors = false;
        foreach (var item in items)
        {
            (bool isValid, IList<ValidationFailure> errors) validationResult = (false, new List<ValidationFailure>());
            if (await BeforeValidateAsync(item, validationResult, true, cancellationToken))
            {
                validationResult = await ValidateAsync(item, true, cancellationToken);
                if (validationResult.isValid)
                {
                    if (await BeforeSaveAsync(item, validationResult, true, cancellationToken))
                    {
                        await DoAddAsync(item, cancellationToken);
                    }
                }
            }

            hasErrors = !validationResult.isValid;
            if (!hasErrors)
            {
                await SaveAsync(new RepositoryRecord<TEntity, TEntityPk>(item), cancellationToken);
            }

            results.Add(new AddOrUpdateOperationResult<TEntity, TEntityPk>(item, validationResult.errors.ToArray(),
                Array.Empty<PropertyChange>()));
        }

        if (!hasErrors)
        {
            await CommitBatchAsync(cancellationToken);
        }
        else
        {
            await RollbackBatchAsync(cancellationToken);
        }

        return results.ToArray();
    }

    public virtual Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync(TEntity entity,
        CancellationToken cancellationToken = default) => UpdateAsync(entity, null, cancellationToken);

    public Task<AddOrUpdateOperationResult<TEntity, TEntityPk>[]> UpdateAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(entities.Select(e => (e, (TEntity?)null)), cancellationToken);

    public virtual async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync(TEntity entity,
        TEntity? oldEntity,
        CancellationToken cancellationToken = default)
    {
        var result = await UpdateAsync(new[] { (entity, oldEntity) }, cancellationToken);
        return result.First();
    }

    public virtual async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>[]> UpdateAsync(
        IEnumerable<(TEntity entity, TEntity? oldEntity)> entities,
        CancellationToken cancellationToken = default)
    {
        var results = new List<AddOrUpdateOperationResult<TEntity, TEntityPk>>();
        await BeginBatchAsync(cancellationToken);
        var hasErrors = false;
        foreach (var entityTuple in entities)
        {
            var changes = Array.Empty<PropertyChange>();
            (bool isValid, IList<ValidationFailure> errors) validationResult = (false, new List<ValidationFailure>());
            if (await BeforeValidateAsync(entityTuple.entity, validationResult, false, cancellationToken))
            {
                validationResult = await ValidateAsync(entityTuple.entity, false, cancellationToken);
                if (validationResult.isValid)
                {
                    if (await BeforeSaveAsync(entityTuple.entity, validationResult, false, cancellationToken))
                    {
                        changes = await DoUpdateAsync(entityTuple.entity, entityTuple.oldEntity, cancellationToken);
                        if (!hasErrors)
                        {
                            await SaveAsync(
                                new RepositoryRecord<TEntity, TEntityPk>(entityTuple.entity, false, changes),
                                cancellationToken);
                        }
                    }
                }
            }

            var result =
                new AddOrUpdateOperationResult<TEntity, TEntityPk>(entityTuple.entity,
                    validationResult.errors.ToArray(),
                    changes);
            results.Add(result);
            hasErrors = !result.IsSuccess;
        }

        if (!hasErrors)
        {
            await CommitBatchAsync(cancellationToken);
        }
        else
        {
            await RollbackBatchAsync(cancellationToken);
        }

        return results.ToArray();
    }

    public virtual Task<bool> DeleteAsync(TEntityPk id, CancellationToken cancellationToken = default) =>
        DeleteAsync(new[] { id }, cancellationToken);

    public virtual async Task<bool> DeleteAsync(IEnumerable<TEntityPk> ids,
        CancellationToken cancellationToken = default)
    {
        var items = await GetByIdsAsync(ids.ToArray(), cancellationToken);
        return await DeleteAsync(items, cancellationToken);
    }

    public virtual Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        DeleteAsync(new[] { entity }, cancellationToken);

    public virtual async Task<bool> DeleteAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        await BeginBatchAsync(cancellationToken);
        foreach (var entity in entities)
        {
            await BeforeDeleteAsync(entity, cancellationToken);
            await DoDeleteAsync(entity, cancellationToken);
            await DoSaveAsync(cancellationToken);
        }

        return await CommitBatchAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> GetAsync(CancellationToken cancellationToken = default)
    {
        var entity = await DoGetAsync(await CreateRepositoryQueryAsync(cancellationToken), cancellationToken);
        if (entity is not null)
        {
            await AfterLoadEntityAsync(entity, cancellationToken);
        }

        return entity;
    }

    public virtual async Task<TEntity?> GetAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        await query.ConfigureAsync(configureQuery, cancellationToken);
        var entity = await DoGetAsync(query, cancellationToken);
        if (entity is not null)
        {
            await AfterLoadEntityAsync(entity, cancellationToken);
        }

        return entity;
    }

    public virtual async Task<TEntity?> GetAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Configure(configureQuery);
        var entity = await DoGetAsync(query, cancellationToken);
        if (entity is not null)
        {
            await AfterLoadEntityAsync(entity, cancellationToken);
        }

        return entity;
    }

    public virtual async Task<(TEntity[] items, int itemsCount)> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);

        var result = await DoGetAllAsync(query, cancellationToken);

        var itemsCount = result.needCount && (query.Offset > 0 || result.items.Length == query.Limit)
            ? await CountAsync(cancellationToken)
            : result.items.Length;
        await AfterLoadEntitiesAsync(result.items, cancellationToken);

        return (result.items, itemsCount);
    }

    public virtual async Task<(TEntity[] items, int itemsCount)> GetAllAsync(
        Action<IRepositoryQuery<TEntity>> configureQuery, CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Configure(configureQuery);

        var result = await DoGetAllAsync(query, cancellationToken);

        var itemsCount = result.needCount && (query.Offset > 0 || result.items.Length == query.Limit)
            ? await CountAsync(configureQuery, cancellationToken)
            : result.items.Length;
        await AfterLoadEntitiesAsync(result.items, cancellationToken);

        return (result.items, itemsCount);
    }

    public virtual async Task<(TEntity[] items, int itemsCount)> GetAllAsync(
        Func<IRepositoryQuery<TEntity>, Task> configureQuery, CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        await query.ConfigureAsync(configureQuery, cancellationToken);

        var result = await DoGetAllAsync(query, cancellationToken);

        var itemsCount = result.needCount && (query.Offset > 0 || result.items.Length == query.Limit)
            ? await CountAsync(configureQuery, cancellationToken)
            : result.items.Length;
        await AfterLoadEntitiesAsync(result.items, cancellationToken);

        return (result.items, itemsCount);
    }

    public virtual async Task<int> SumAsync(Expression<Func<TEntity, int>> selector,
        CancellationToken cancellationToken = default) =>
        await DoSumAsync(await CreateRepositoryQueryAsync(cancellationToken), selector, cancellationToken);

    public virtual async Task<int> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, int>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        await query.ConfigureAsync(configureQuery, cancellationToken);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<int> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, int>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Configure(configureQuery);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<long> SumAsync(Expression<Func<TEntity, long>> selector,
        CancellationToken cancellationToken = default) =>
        await DoSumAsync(await CreateRepositoryQueryAsync(cancellationToken), selector, cancellationToken);

    public virtual async Task<long> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, long>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        await query.ConfigureAsync(configureQuery, cancellationToken);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<long> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, long>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Configure(configureQuery);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<double> SumAsync(Expression<Func<TEntity, double>> selector,
        CancellationToken cancellationToken = default) =>
        await DoSumAsync(await CreateRepositoryQueryAsync(cancellationToken), selector, cancellationToken);

    public virtual async Task<double> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, double>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        await query.ConfigureAsync(configureQuery, cancellationToken);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<double> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, double>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Configure(configureQuery);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<float> SumAsync(Expression<Func<TEntity, float>> selector,
        CancellationToken cancellationToken = default) =>
        await DoSumAsync(await CreateRepositoryQueryAsync(cancellationToken), selector, cancellationToken);

    public virtual async Task<float> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, float>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        await query.ConfigureAsync(configureQuery, cancellationToken);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<float> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, float>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Configure(configureQuery);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector,
        CancellationToken cancellationToken = default) =>
        await DoSumAsync(await CreateRepositoryQueryAsync(cancellationToken), selector, cancellationToken);

    public virtual async Task<decimal> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, decimal>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        await query.ConfigureAsync(configureQuery, cancellationToken);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<decimal> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, decimal>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Configure(configureQuery);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<int?> SumAsync(Expression<Func<TEntity, int?>> selector,
        CancellationToken cancellationToken = default) =>
        await DoSumAsync(await CreateRepositoryQueryAsync(cancellationToken), selector, cancellationToken);

    public virtual async Task<int?> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, int?>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        await query.ConfigureAsync(configureQuery, cancellationToken);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<int?> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, int?>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Configure(configureQuery);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<long?> SumAsync(Expression<Func<TEntity, long?>> selector,
        CancellationToken cancellationToken = default) =>
        await DoSumAsync(await CreateRepositoryQueryAsync(cancellationToken), selector, cancellationToken);

    public virtual async Task<long?> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, long?>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        await query.ConfigureAsync(configureQuery, cancellationToken);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<long?> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, long?>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Configure(configureQuery);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<double?> SumAsync(Expression<Func<TEntity, double?>> selector,
        CancellationToken cancellationToken = default) =>
        await DoSumAsync(await CreateRepositoryQueryAsync(cancellationToken), selector, cancellationToken);

    public virtual async Task<double?> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, double?>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        await query.ConfigureAsync(configureQuery, cancellationToken);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<double?> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, double?>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Configure(configureQuery);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<float?> SumAsync(Expression<Func<TEntity, float?>> selector,
        CancellationToken cancellationToken = default) =>
        await DoSumAsync(await CreateRepositoryQueryAsync(cancellationToken), selector, cancellationToken);

    public virtual async Task<float?> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, float?>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        await query.ConfigureAsync(configureQuery, cancellationToken);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<float?> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, float?>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Configure(configureQuery);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<decimal?> SumAsync(Expression<Func<TEntity, decimal?>> selector,
        CancellationToken cancellationToken = default) =>
        await DoSumAsync(await CreateRepositoryQueryAsync(cancellationToken), selector, cancellationToken);

    public virtual async Task<decimal?> SumAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        Expression<Func<TEntity, decimal?>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        await query.ConfigureAsync(configureQuery, cancellationToken);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<decimal?> SumAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        Expression<Func<TEntity, decimal?>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Configure(configureQuery);
        return await DoSumAsync(query, selector, cancellationToken);
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        await DoCountAsync(await CreateRepositoryQueryAsync(cancellationToken), cancellationToken);

    public virtual async Task<int> CountAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        await query.ConfigureAsync(configureQuery, cancellationToken);
        return await DoCountAsync(query, cancellationToken);
    }

    public virtual async Task<int> CountAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Configure(configureQuery);
        return await DoCountAsync(query, cancellationToken);
    }

    public virtual async Task<TEntity?> GetByIdAsync(TEntityPk id, CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Where(i => i.Id!.Equals(id));

        var entity = await DoGetAsync(query, cancellationToken);
        if (entity is not null)
        {
            await AfterLoadEntityAsync(entity, cancellationToken);
        }

        return entity;
    }

    public virtual async Task<TEntity?> GetByIdAsync(TEntityPk id,
        Func<IRepositoryQuery<TEntity>, Task> configureQuery, CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Where(i => i.Id!.Equals(id));
        await query.ConfigureAsync(configureQuery, cancellationToken);

        var entity = await DoGetAsync(query, cancellationToken);
        if (entity is not null)
        {
            await AfterLoadEntityAsync(entity, cancellationToken);
        }

        return entity;
    }

    public virtual async Task<TEntity?> GetByIdAsync(TEntityPk id, Action<IRepositoryQuery<TEntity>> configureQuery,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Where(i => i.Id!.Equals(id));
        query.Configure(configureQuery);

        var entity = await DoGetAsync(query, cancellationToken);
        if (entity is not null)
        {
            await AfterLoadEntityAsync(entity, cancellationToken);
        }

        return entity;
    }

    public virtual async Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Where(i => ids.Contains(i.Id));

        var result = await DoGetAllAsync(query, cancellationToken);

        await AfterLoadEntitiesAsync(result.items, cancellationToken);

        return result.items;
    }

    public virtual async Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids,
        Func<IRepositoryQuery<TEntity>, Task> configureQuery, CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Where(i => ids.Contains(i.Id));
        await query.ConfigureAsync(configureQuery, cancellationToken);

        var result = await DoGetAllAsync(query, cancellationToken);

        await AfterLoadEntitiesAsync(result.items, cancellationToken);

        return result.items;
    }

    public virtual async Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids,
        Action<IRepositoryQuery<TEntity>> configureQuery, CancellationToken cancellationToken = default)
    {
        var query = await CreateRepositoryQueryAsync(cancellationToken);
        query.Where(i => ids.Contains(i.Id)).Configure(configureQuery);

        var result = await DoGetAllAsync(query, cancellationToken);

        await AfterLoadEntitiesAsync(result.items, cancellationToken);

        return result.items;
    }

    protected abstract Task<TQuery> CreateRepositoryQueryAsync(CancellationToken cancellationToken = default);

    protected abstract Task<(TEntity[] items, int itemsCount, bool needCount)> DoGetAllAsync(TQuery query,
        CancellationToken cancellationToken = default);

    protected abstract Task<int> DoCountAsync(TQuery query, CancellationToken cancellationToken = default);

    protected abstract Task<int> DoSumAsync(TQuery query, Expression<Func<TEntity, int>> selector,
        CancellationToken cancellationToken = default);

    protected abstract Task<long> DoSumAsync(TQuery query, Expression<Func<TEntity, long>> selector,
        CancellationToken cancellationToken = default);

    protected abstract Task<double> DoSumAsync(TQuery query, Expression<Func<TEntity, double>> selector,
        CancellationToken cancellationToken = default);

    protected abstract Task<float> DoSumAsync(TQuery query, Expression<Func<TEntity, float>> selector,
        CancellationToken cancellationToken = default);

    protected abstract Task<decimal> DoSumAsync(TQuery query, Expression<Func<TEntity, decimal>> selector,
        CancellationToken cancellationToken = default);

    protected abstract Task<int?> DoSumAsync(TQuery query, Expression<Func<TEntity, int?>> selector,
        CancellationToken cancellationToken = default);

    protected abstract Task<long?> DoSumAsync(TQuery query, Expression<Func<TEntity, long?>> selector,
        CancellationToken cancellationToken = default);

    protected abstract Task<double?> DoSumAsync(TQuery query, Expression<Func<TEntity, double?>> selector,
        CancellationToken cancellationToken = default);

    protected abstract Task<float?> DoSumAsync(TQuery query, Expression<Func<TEntity, float?>> selector,
        CancellationToken cancellationToken = default);

    protected abstract Task<decimal?> DoSumAsync(TQuery query, Expression<Func<TEntity, decimal?>> selector,
        CancellationToken cancellationToken = default);

    protected abstract Task<TEntity?> DoGetAsync(TQuery query, CancellationToken cancellationToken = default);

    protected abstract Task DoSaveAsync(CancellationToken cancellationToken = default);

    protected abstract Task<PropertyChange[]> GetChangesAsync(TEntity item);

    protected abstract Task DoAddAsync(TEntity entity, CancellationToken cancellationToken = default);

    protected abstract Task<PropertyChange[]> DoUpdateAsync(TEntity entity, TEntity? oldEntity,
        CancellationToken cancellationToken = default);

    protected abstract Task DoDeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

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

    protected virtual Task AfterLoadAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        AfterLoadAsync(new[] { entity }, cancellationToken);

    protected virtual Task AfterLoadAsync(TEntity[] entities, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    protected virtual Task<bool> BeforeSaveAsync(TEntity item,
        (bool isValid, IList<ValidationFailure> errors) validationResult, bool isNew,
        CancellationToken cancellationToken = default) =>
        FiltersManager.BeforeSaveAsync<TEntity, TEntityPk>(item, validationResult, isNew, cancellationToken);

    protected virtual async Task<bool> AfterSaveAsync(IEnumerable<RepositoryRecord<TEntity, TEntityPk>> items,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            await FiltersManager.AfterSaveAsync<TEntity, TEntityPk>(item.Item, item.IsNew, item.Changes,
                cancellationToken);
        }

        return true;
    }

    protected virtual Task BeforeDeleteAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    protected virtual Task CheckAccessAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        CheckAccessAsync(new[] { entity }, cancellationToken);

    protected virtual async Task CheckAccessAsync(TEntity[] entities, CancellationToken cancellationToken = default)
    {
        foreach (var accessChecker in AccessCheckers)
        {
            await accessChecker.CheckAccessAsync(entities, cancellationToken);
        }
    }

    protected virtual async Task<(bool isValid, IList<ValidationFailure> errors)> ValidateAsync(TEntity entity,
        bool isNew, CancellationToken cancellationToken = default)
    {
        var result = await FluentGraphValidator.TryValidateModelAsync(entity, cancellationToken);
        return (result.IsValid, result.Results.SelectMany(r => r.Errors).ToList());
    }

    protected virtual Task<bool> BeforeValidateAsync(TEntity item,
        (bool isValid, IList<ValidationFailure> errors) validationResult,
        bool isNew, CancellationToken cancellationToken = default) =>
        FiltersManager.BeforeValidateAsync<TEntity, TEntityPk>(item, validationResult, isNew,
            cancellationToken);
}
