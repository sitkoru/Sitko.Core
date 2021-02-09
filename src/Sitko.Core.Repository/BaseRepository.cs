using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Repository
{
    public interface IRepositoryContext<TEntity, TEntityPk> where TEntity : class, IEntity<TEntityPk>
    {
        RepositoryFiltersManager FiltersManager { get; }
        List<IValidator>? Validators { get; }
        List<IAccessChecker<TEntity, TEntityPk>>? AccessCheckers { get; }
        ILogger<IRepository<TEntity, TEntityPk>> Logger { get; }
    }

    public abstract class BaseRepository<TEntity, TEntityPk, TQuery> : IRepository<TEntity, TEntityPk>
        where TEntity : class, IEntity<TEntityPk> where TQuery : IRepositoryQuery<TEntity>
    {
        protected readonly IValidator[] Validators;
        protected readonly RepositoryFiltersManager FiltersManager;
        protected readonly List<IAccessChecker<TEntity, TEntityPk>> AccessCheckers;
        protected readonly ILogger Logger;
        private List<RepositoryRecord<TEntity, TEntityPk>>? _batch;

        protected BaseRepository(IRepositoryContext<TEntity, TEntityPk> repositoryContext)
        {
            Validators = repositoryContext.Validators?.ToArray() ?? new IValidator[0];
            FiltersManager = repositoryContext.FiltersManager;
            AccessCheckers = repositoryContext.AccessCheckers ?? new List<IAccessChecker<TEntity, TEntityPk>>();
            Logger = repositoryContext.Logger;
        }

        protected abstract Task<TQuery> CreateRepositoryQueryAsync(CancellationToken cancellationToken = default);

        protected abstract Task<(TEntity[] items, bool needCount)> DoGetAllAsync(TQuery query,
            CancellationToken cancellationToken = default);

        protected abstract Task<int> DoCountAsync(TQuery query, CancellationToken cancellationToken = default);
        protected abstract Task<TEntity?> DoGetAsync(TQuery query, CancellationToken cancellationToken = default);

        protected abstract Task DoSaveAsync(CancellationToken cancellationToken = default);

        public abstract PropertyChange[] GetChanges(TEntity item, TEntity oldEntity);

        public abstract Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default);

        public abstract Task<bool> CommitTransactionAsync(CancellationToken cancellationToken = default);
        public abstract Task<bool> RollbackTransactionAsync(CancellationToken cancellationToken = default);

        public abstract Task RefreshAsync(TEntity entity, CancellationToken cancellationToken = default);


        public virtual Task<bool> BeginBatchAsync(CancellationToken cancellationToken = default)
        {
            if (_batch != null)
            {
                return Task.FromResult(false);
            }

            _batch = new List<RepositoryRecord<TEntity, TEntityPk>>();

            return Task.FromResult(true);
        }

        public virtual async Task<bool> CommitBatchAsync(CancellationToken cancellationToken = default)
        {
            if (_batch == null)
            {
                return false;
            }

            await DoSaveAsync();
            await AfterSaveAsync(_batch);

            _batch = null;
            return true;
        }

        public virtual Task<bool> RollbackBatchAsync(CancellationToken cancellationToken = default)
        {
            if (_batch == null)
            {
                return Task.FromResult(false);
            }

            _batch = null;
            return Task.FromResult(true);
        }

        protected abstract Task DoAddAsync(TEntity item, CancellationToken cancellationToken = default);
        protected abstract Task DoUpdateAsync(TEntity item, CancellationToken cancellationToken = default);
        protected abstract Task DoDeleteAsync(TEntity item, CancellationToken cancellationToken = default);
        protected abstract Task<TEntity> GetOldItemAsync(TEntityPk id, CancellationToken cancellationToken = default);

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

        private async Task SaveAsync(RepositoryRecord<TEntity, TEntityPk> record,
            CancellationToken cancellationToken = default)
        {
            if (_batch == null)
            {
                await DoSaveAsync(cancellationToken);
                await AfterSaveAsync(new[] {record}, cancellationToken);
            }
            else
            {
                _batch.Add(record);
            }
        }

        public virtual async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync(TEntity item,
            CancellationToken cancellationToken = default)
        {
            var oldItem = await GetOldItemAsync(item.Id, cancellationToken);
            PropertyChange[] changes = new PropertyChange[0];
            (bool isValid, IList<ValidationFailure> errors) validationResult = (false, new List<ValidationFailure>());
            if (await BeforeValidateAsync(item, validationResult, false, cancellationToken))
            {
                changes = GetChanges(item, oldItem);
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
            if (_batch == null)
            {
                await DoSaveAsync(cancellationToken);
            }

            return true;
        }

        public virtual async Task<TEntity?> GetAsync(CancellationToken cancellationToken = default)
        {
            return await DoGetAsync(await CreateRepositoryQueryAsync(cancellationToken), cancellationToken);
        }

        public virtual async Task<TEntity?> GetAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery,
            CancellationToken cancellationToken = default)
        {
            var query = await CreateRepositoryQueryAsync(cancellationToken);
            await query.ConfigureAsync(configureQuery, cancellationToken);
            return await DoGetAsync(query, cancellationToken);
        }

        public virtual async Task<TEntity?> GetAsync(Action<IRepositoryQuery<TEntity>> configureQuery,
            CancellationToken cancellationToken = default)
        {
            var query = await CreateRepositoryQueryAsync(cancellationToken);
            query.Configure(configureQuery);
            return await DoGetAsync(query, cancellationToken);
        }

        public virtual async Task<(TEntity[] items, int itemsCount)> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            var query = await CreateRepositoryQueryAsync(cancellationToken);

            (TEntity[] items, bool needCount) = await DoGetAllAsync(query, cancellationToken);

            var itemsCount = needCount && (query.Offset > 0 || items.Length == query.Limit)
                ? await CountAsync(cancellationToken)
                : items.Length;
            await AfterLoadAsync(items, cancellationToken);

            return (items, itemsCount);
        }

        public virtual async Task<(TEntity[] items, int itemsCount)> GetAllAsync(
            Action<IRepositoryQuery<TEntity>> configureQuery, CancellationToken cancellationToken = default)
        {
            var query = await CreateRepositoryQueryAsync(cancellationToken);
            query.Configure(configureQuery);

            (TEntity[] items, bool needCount) = await DoGetAllAsync(query, cancellationToken);

            var itemsCount = needCount && (query.Offset > 0 || items.Length == query.Limit)
                ? await CountAsync(configureQuery, cancellationToken)
                : items.Length;
            await AfterLoadAsync(items, cancellationToken);

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
            await AfterLoadAsync(items, cancellationToken);

            return (items, itemsCount);
        }

        public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return await DoCountAsync(await CreateRepositoryQueryAsync(cancellationToken), cancellationToken);
        }

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

            return await DoGetAsync(query, cancellationToken);
        }

        public virtual async Task<TEntity?> GetByIdAsync(TEntityPk id,
            Func<IRepositoryQuery<TEntity>, Task> configureQuery, CancellationToken cancellationToken = default)
        {
            var query = await CreateRepositoryQueryAsync(cancellationToken);
            query.Where(i => i.Id!.Equals(id));
            await query.ConfigureAsync(configureQuery, cancellationToken);

            return await DoGetAsync(query);
        }

        public virtual async Task<TEntity?> GetByIdAsync(TEntityPk id, Action<IRepositoryQuery<TEntity>> configureQuery,
            CancellationToken cancellationToken = default)
        {
            var query = await CreateRepositoryQueryAsync(cancellationToken);
            query.Where(i => i.Id!.Equals(id));
            query.Configure(configureQuery);

            return await DoGetAsync(query, cancellationToken);
        }

        public virtual async Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids,
            CancellationToken cancellationToken = default)
        {
            var query = await CreateRepositoryQueryAsync(cancellationToken);
            query.Where(i => ids.Contains(i.Id));

            (TEntity[] items, _) = await DoGetAllAsync(query, cancellationToken);

            return items;
        }

        public virtual async Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids,
            Func<IRepositoryQuery<TEntity>, Task> configureQuery, CancellationToken cancellationToken = default)
        {
            var query = await CreateRepositoryQueryAsync(cancellationToken);
            query.Where(i => ids.Contains(i.Id));
            await query.ConfigureAsync(configureQuery, cancellationToken);

            (TEntity[] items, _) = await DoGetAllAsync(query, cancellationToken);

            return items;
        }

        public virtual async Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids,
            Action<IRepositoryQuery<TEntity>> configureQuery, CancellationToken cancellationToken = default)
        {
            var query = await CreateRepositoryQueryAsync(cancellationToken);
            query.Where(i => ids.Contains(i.Id)).Configure(configureQuery);

            (TEntity[] items, _) = await DoGetAllAsync(query, cancellationToken);

            return items;
        }

        protected virtual Task AfterLoadAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            return AfterLoadAsync(new[] {entity}, cancellationToken);
        }

        protected virtual Task AfterLoadAsync(TEntity[] entities, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        protected virtual Task<bool> BeforeSaveAsync(TEntity item,
            (bool isValid, IList<ValidationFailure> errors) validationResult, bool isNew,
            PropertyChange[]? changes = null, CancellationToken cancellationToken = default)
        {
            return FiltersManager.BeforeSaveAsync<TEntity, TEntityPk>(item, validationResult, isNew, changes,
                cancellationToken);
        }

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

        protected virtual Task BeforeDeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        protected virtual Task CheckAccessAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            return CheckAccessAsync(new[] {entity}, cancellationToken);
        }

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
            bool isNew, CancellationToken cancellationToken = default)
        {
            return FiltersManager.BeforeValidateAsync<TEntity, TEntityPk>(item, validationResult, isNew,
                cancellationToken);
        }
    }
}
