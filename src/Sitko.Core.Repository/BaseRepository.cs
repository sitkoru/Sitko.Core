using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Linq.Expressions;
using AnyClone;
using JetBrains.Annotations;
using KellermanSoftware.CompareNetObjects;

namespace Sitko.Core.Repository
{
    public interface IRepositoryContext<TEntity, TEntityPk> where TEntity : class, IEntity<TEntityPk>
    {
        RepositoryFiltersManager FiltersManager { get; }
        List<IValidator>? Validators { get; }
        List<IAccessChecker<TEntity, TEntityPk>>? AccessCheckers { get; }
        ILogger<IRepository<TEntity, TEntityPk>> Logger { get; }
        CompareLogic Comparer { get; }
    }

    public abstract class BaseRepository<TEntity, TEntityPk, TQuery> : IRepository<TEntity, TEntityPk>
        where TEntity : class, IEntity<TEntityPk> where TQuery : IRepositoryQuery<TEntity>
    {
        private List<RepositoryRecord<TEntity, TEntityPk>>? batch;
        private readonly Dictionary<TEntityPk, TEntity> snapshots = new();

        protected CompareLogic Comparer { get; }

        protected BaseRepository(IRepositoryContext<TEntity, TEntityPk> repositoryContext)
        {
            Validators = repositoryContext.Validators?.ToArray() ?? new IValidator[0];
            FiltersManager = repositoryContext.FiltersManager;
            AccessCheckers = repositoryContext.AccessCheckers ?? new List<IAccessChecker<TEntity, TEntityPk>>();
            Logger = repositoryContext.Logger;
            Comparer = repositoryContext.Comparer;
        }

        [PublicAPI] protected IValidator[] Validators { get; }

        [PublicAPI] protected RepositoryFiltersManager FiltersManager { get; }

        [PublicAPI] protected List<IAccessChecker<TEntity, TEntityPk>> AccessCheckers { get; }

        protected ILogger Logger { get; }

        public abstract Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default);

        public abstract Task<bool> CommitTransactionAsync(CancellationToken cancellationToken = default);
        public abstract Task<bool> RollbackTransactionAsync(CancellationToken cancellationToken = default);

        public abstract Task RefreshAsync(TEntity entity, CancellationToken cancellationToken = default);

        public async Task<bool> HasChangesAsync(TEntity entity)
        {
            var changesResult = await GetChangesAsync(entity);
            return changesResult.changes.Length > 0;
        }

        public TEntity CreateSnapshot(TEntity entity) => entity.Clone();


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
            (bool isValid, IList<ValidationFailure> errors) validationResult = (false, new List<ValidationFailure>());
            if (await BeforeValidateAsync(item, validationResult, true, cancellationToken))
            {
                validationResult = await ValidateAsync(item, true, cancellationToken: cancellationToken);
                if (validationResult.isValid)
                {
                    if (await BeforeSaveAsync(item, validationResult, true, cancellationToken: cancellationToken))
                    {
                        await DoAddAsync(item, cancellationToken);
                    }
                }
            }

            if (validationResult.isValid)
            {
                await SaveAsync(new RepositoryRecord<TEntity, TEntityPk>(item), cancellationToken);
            }

            return new AddOrUpdateOperationResult<TEntity, TEntityPk>(item, validationResult.errors,
                new PropertyChange[0]);
        }

        public virtual async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync(TEntity item,
            CancellationToken cancellationToken = default)
        {
            PropertyChange[] changes = new PropertyChange[0];
            (bool isValid, IList<ValidationFailure> errors) validationResult = (false, new List<ValidationFailure>());
            if (await BeforeValidateAsync(item, validationResult, false, cancellationToken))
            {
                var changesResult = await GetChangesAsync(item);
                changes = changesResult.changes;
                var oldItem = changesResult.oldEntity;
                if (changes.Any())
                {
                    validationResult = await ValidateAsync(item, false, changes, cancellationToken);
                    if (validationResult.isValid)
                    {
                        if (await BeforeSaveAsync(item, validationResult, false, changes, cancellationToken))
                        {
                            await DoUpdateAsync(item, cancellationToken);

                            await SaveAsync(new RepositoryRecord<TEntity, TEntityPk>(item, false, changes, oldItem),
                                cancellationToken);
                        }
                    }
                }
            }


            return new AddOrUpdateOperationResult<TEntity, TEntityPk>(item, validationResult.errors, changes);
        }

        public virtual async Task<bool> DeleteAsync(TEntityPk id, CancellationToken cancellationToken = default)
        {
            var item = await GetByIdAsync(id, cancellationToken);
            if (item == null)
            {
                return false;
            }

            return await DeleteAsync(item, cancellationToken);
        }

        public virtual async Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await BeforeDeleteAsync(entity, cancellationToken);
            await DoDeleteAsync(entity, cancellationToken);
            if (batch == null)
            {
                await DoSaveAsync(cancellationToken);
            }

            return true;
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

            (TEntity[] items, var needCount) = await DoGetAllAsync(query, cancellationToken);

            var itemsCount = needCount && (query.Offset > 0 || items.Length == query.Limit)
                ? await CountAsync(cancellationToken)
                : items.Length;
            await AfterLoadEntitiesAsync(items, cancellationToken);

            return (items, itemsCount);
        }

        public virtual async Task<(TEntity[] items, int itemsCount)> GetAllAsync(
            Action<IRepositoryQuery<TEntity>> configureQuery, CancellationToken cancellationToken = default)
        {
            var query = await CreateRepositoryQueryAsync(cancellationToken);
            query.Configure(configureQuery);

            (TEntity[] items, var needCount) = await DoGetAllAsync(query, cancellationToken);

            var itemsCount = needCount && (query.Offset > 0 || items.Length == query.Limit)
                ? await CountAsync(configureQuery, cancellationToken)
                : items.Length;
            await AfterLoadEntitiesAsync(items, cancellationToken);

            return (items, itemsCount);
        }

        public virtual async Task<(TEntity[] items, int itemsCount)> GetAllAsync(
            Func<IRepositoryQuery<TEntity>, Task> configureQuery, CancellationToken cancellationToken = default)
        {
            var query = await CreateRepositoryQueryAsync(cancellationToken);
            await query.ConfigureAsync(configureQuery, cancellationToken);
            var (items, needCount) = await DoGetAllAsync(query, cancellationToken);

            var itemsCount = needCount && (query.Offset > 0 || items.Length == query.Limit)
                ? await CountAsync(configureQuery, cancellationToken)
                : items.Length;
            await AfterLoadEntitiesAsync(items, cancellationToken);

            return (items, itemsCount);
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

            (TEntity[] items, _) = await DoGetAllAsync(query, cancellationToken);

            await AfterLoadEntitiesAsync(items, cancellationToken);

            return items;
        }

        public virtual async Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids,
            Func<IRepositoryQuery<TEntity>, Task> configureQuery, CancellationToken cancellationToken = default)
        {
            var query = await CreateRepositoryQueryAsync(cancellationToken);
            query.Where(i => ids.Contains(i.Id));
            await query.ConfigureAsync(configureQuery, cancellationToken);

            (TEntity[] items, _) = await DoGetAllAsync(query, cancellationToken);

            await AfterLoadEntitiesAsync(items, cancellationToken);

            return items;
        }

        public virtual async Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids,
            Action<IRepositoryQuery<TEntity>> configureQuery, CancellationToken cancellationToken = default)
        {
            var query = await CreateRepositoryQueryAsync(cancellationToken);
            query.Where(i => ids.Contains(i.Id)).Configure(configureQuery);

            (TEntity[] items, _) = await DoGetAllAsync(query, cancellationToken);

            await AfterLoadEntitiesAsync(items, cancellationToken);

            return items;
        }

        protected abstract Task<TQuery> CreateRepositoryQueryAsync(CancellationToken cancellationToken = default);

        protected abstract Task<(TEntity[] items, bool needCount)> DoGetAllAsync(TQuery query,
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

        protected virtual Task<(PropertyChange[] changes, TEntity oldEntity)> GetChangesAsync(TEntity item)
        {
            var changes = new List<PropertyChange>();
            var snapshot  = snapshots[item.Id];
            var differences = Comparer.Compare(snapshot, item);
            if (!differences.AreEqual)
            {
                foreach (var difference in differences.Differences)
                {
                    if (difference.Object1 is null || difference.Object2 is null)
                    {
                        var propertyName = !string.IsNullOrEmpty(difference.ParentPropertyName)
                            ? difference.ParentPropertyName
                            : !string.IsNullOrEmpty(difference.PropertyName)
                                ? difference.PropertyName
                                : null;
                        if (propertyName is not null)
                        {
                            var property = difference.ParentObject1.GetType().GetProperty(propertyName);
                            if (property is not null)
                            {
                                if (property.PropertyType != typeof(string) &&
                                    typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    changes.Add(new PropertyChange(difference.PropertyName, difference.Object1, difference.Object2));
                }
            }

            return Task.FromResult((changes.ToArray(), snapshot));
        }

        protected abstract Task DoAddAsync(TEntity item, CancellationToken cancellationToken = default);
        protected abstract Task DoUpdateAsync(TEntity item, CancellationToken cancellationToken = default);
        protected abstract Task DoDeleteAsync(TEntity item, CancellationToken cancellationToken = default);

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
            SaveSnapshots(entities);
            return AfterLoadAsync(entities, cancellationToken);
        }

        private Task AfterLoadEntitiesAsync(TEntity[] entities, CancellationToken cancellationToken = default)
        {
            SaveSnapshots(entities);
            return AfterLoadAsync(entities, cancellationToken);
        }

        protected virtual Task AfterLoadAsync(TEntity entity, CancellationToken cancellationToken = default) =>
            AfterLoadAsync(new[] { entity }, cancellationToken);

        protected virtual Task AfterLoadAsync(TEntity[] entities, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        protected void SaveSnapshot(TEntity entity) => SaveSnapshots(new[] { entity });

        protected void SaveSnapshots(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                var snapshot = CreateSnapshot(entity);
                snapshots[entity.Id] = snapshot;
            }
        }

        protected virtual Task<bool> BeforeSaveAsync(TEntity item,
            (bool isValid, IList<ValidationFailure> errors) validationResult, bool isNew,
            PropertyChange[]? changes = null, CancellationToken cancellationToken = default) =>
            FiltersManager.BeforeSaveAsync<TEntity, TEntityPk>(item, validationResult, isNew, changes,
                cancellationToken);

        protected virtual async Task<bool> AfterSaveAsync(IEnumerable<RepositoryRecord<TEntity, TEntityPk>> items,
            CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                SaveSnapshot(item.Item);
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
            bool isNew,
            PropertyChange[]? changes = null, CancellationToken cancellationToken = default)
        {
            var failures = new List<ValidationFailure>();
            foreach (var validator in Validators.Where(v => v.CanValidateInstancesOfType(typeof(TEntity))))
            {
                var result =
                    await validator.ValidateAsync(new ValidationContext<TEntity>(entity), cancellationToken);
                if (!result.IsValid)
                {
                    failures.AddRange(result.Errors);
                }
            }

            return (!failures.Any(), failures);
        }

        protected virtual Task<bool> BeforeValidateAsync(TEntity item,
            (bool isValid, IList<ValidationFailure> errors) validationResult,
            bool isNew, CancellationToken cancellationToken = default) =>
            FiltersManager.BeforeValidateAsync<TEntity, TEntityPk>(item, validationResult, isNew,
                cancellationToken);
    }
}
