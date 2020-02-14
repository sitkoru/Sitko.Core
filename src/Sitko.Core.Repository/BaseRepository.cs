using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Repository
{
    public interface IRepositoryContext<TEntity, TEntityPk> where TEntity : class, IEntity<TEntityPk>
    {
        RepositoryFiltersManager FiltersManager { get; }
        List<IValidator<TEntity>> Validators { get; }
        List<IAccessChecker<TEntity, TEntityPk>> AccessCheckers { get; }
        ILogger<IRepository<TEntity, TEntityPk>> Logger { get; }
    }

    public abstract class BaseRepository<TEntity, TEntityPk, TQuery> : IRepository<TEntity, TEntityPk>
        where TEntity : class, IEntity<TEntityPk> where TQuery : IRepositoryQuery<TEntity>
    {
        protected readonly List<IValidator<TEntity>> Validators;
        protected readonly RepositoryFiltersManager FiltersManager;
        protected readonly List<IAccessChecker<TEntity, TEntityPk>> AccessCheckers;
        protected readonly ILogger Logger;

        protected BaseRepository(IRepositoryContext<TEntity, TEntityPk> repositoryContext)
        {
            Validators = repositoryContext.Validators ?? new List<IValidator<TEntity>>();
            FiltersManager = repositoryContext.FiltersManager;
            AccessCheckers = repositoryContext.AccessCheckers ?? new List<IAccessChecker<TEntity, TEntityPk>>();
            Logger = repositoryContext.Logger;

            Init();
        }

        private void Init()
        {
            RegisterValidators();
        }

        protected virtual void RegisterValidators()
        {
        }

        protected abstract Task<TQuery> CreateRepositoryQueryAsync();
        protected abstract Task<(TEntity[] items, bool needCount)> DoGetAllAsync(TQuery query);
        protected abstract Task<int> DoCountAsync(TQuery query);
        protected abstract Task<TEntity?> DoGetAsync(TQuery query);

        protected abstract Task DoSaveAsync(TEntity item, bool isNew, PropertyChange[]? changes = null,
            TEntity? oldItem = null);

        public abstract PropertyChange[] GetChanges(TEntity item, TEntity oldEntity);

        public abstract Task<bool> BeginTransactionAsync();

        public abstract Task<bool> CommitTransactionAsync();

        protected abstract Task DoAddAsync(TEntity item);
        protected abstract Task DoUpdateAsync(TEntity item);
        protected abstract Task DoDeleteAsync(TEntity item);
        protected abstract Task<TEntity> GetOldItem(TEntityPk id);

        public virtual async Task<TEntity> NewAsync()
        {
            var item = Activator.CreateInstance<TEntity>();
            await AfterLoadAsync(item);
            return item;
        }

        public virtual async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddAsync(TEntity item)
        {
            (bool isValid, IList<ValidationFailure> errors) validationResult = (false, new List<ValidationFailure>());
            if (await BeforeValidateAsync(item, validationResult, true))
            {
                validationResult = await ValidateAsync(item, true);
                if (validationResult.isValid)
                {
                    if (await BeforeSaveAsync(item, validationResult, true))
                    {
                        await DoAddAsync(item);
                    }
                }
            }

            if (validationResult.isValid)
            {
                await DoSaveAsync(item, true);
            }

            return new AddOrUpdateOperationResult<TEntity, TEntityPk>(item, validationResult.errors,
                new PropertyChange[0]);
        }

        public virtual async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync(TEntity item)
        {
            var oldItem = await GetOldItem(item.Id);
            var changes = GetChanges(item, oldItem);
            (bool isValid, IList<ValidationFailure> errors) validationResult = (false, new List<ValidationFailure>());
            if (await BeforeValidateAsync(item, validationResult, false, changes))
            {
                validationResult = await ValidateAsync(item, false, changes);
                if (validationResult.isValid)
                {
                    if (await BeforeSaveAsync(item, validationResult, false, changes))
                    {
                        await DoUpdateAsync(item);
                    }
                }
            }

            if (validationResult.isValid)
            {
                await DoSaveAsync(item, false, changes, oldItem);
            }

            return new AddOrUpdateOperationResult<TEntity, TEntityPk>(item, validationResult.errors, changes);
        }

        public virtual async Task<bool> DeleteAsync(TEntityPk id)
        {
            var item = await GetByIdAsync(id);

            return await DeleteAsync(item);
        }

        public virtual async Task<bool> DeleteAsync(TEntity entity)
        {
            if (entity != null)
            {
                await BeforeDeleteAsync(entity);
                await DoDeleteAsync(entity);
                return true;
            }

            return false;
        }

        public virtual async Task<TEntity?> GetAsync()
        {
            return await DoGetAsync(await CreateRepositoryQueryAsync());
        }

        public virtual async Task<TEntity?> GetAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery)
        {
            var query = await CreateRepositoryQueryAsync();
            await query.ConfigureAsync(configureQuery);
            return await DoGetAsync(query);
        }

        public virtual async Task<TEntity?> GetAsync(Action<IRepositoryQuery<TEntity>> configureQuery)
        {
            var query = await CreateRepositoryQueryAsync();
            query.Configure(configureQuery);
            return await DoGetAsync(query);
        }

        public virtual async Task<(TEntity[] items, int itemsCount)> GetAllAsync()
        {
            var query = await CreateRepositoryQueryAsync();

            (TEntity[] items, bool needCount) = await DoGetAllAsync(query);

            var itemsCount = needCount && (query.Offset > 0 || items.Length == query.Limit)
                ? await CountAsync()
                : items.Length;
            await AfterLoadAsync(items);

            return (items, itemsCount);
        }

        public virtual async Task<(TEntity[] items, int itemsCount)> GetAllAsync(
            Action<IRepositoryQuery<TEntity>> configureQuery)
        {
            var query = await CreateRepositoryQueryAsync();
            query.Configure(configureQuery);

            (TEntity[] items, bool needCount) = await DoGetAllAsync(query);

            var itemsCount = needCount && (query.Offset > 0 || items.Length == query.Limit)
                ? await CountAsync(configureQuery)
                : items.Length;
            await AfterLoadAsync(items);

            return (items, itemsCount);
        }

        public virtual async Task<(TEntity[] items, int itemsCount)> GetAllAsync(
            Func<IRepositoryQuery<TEntity>, Task> configureQuery)
        {
            var query = await CreateRepositoryQueryAsync();
            await query.ConfigureAsync(configureQuery);
            var (items, needCount) = await DoGetAllAsync(query);

            var itemsCount = needCount && (query.Offset > 0 || items.Length == query.Limit)
                ? await CountAsync(configureQuery)
                : items.Length;
            await AfterLoadAsync(items);

            return (items, itemsCount);
        }

        public virtual async Task<int> CountAsync()
        {
            return await DoCountAsync(await CreateRepositoryQueryAsync());
        }

        public virtual async Task<int> CountAsync(Func<IRepositoryQuery<TEntity>, Task> configureQuery)
        {
            var query = await CreateRepositoryQueryAsync();
            await query.ConfigureAsync(configureQuery);
            return await DoCountAsync(query);
        }

        public virtual async Task<int> CountAsync(Action<IRepositoryQuery<TEntity>> configureQuery)
        {
            var query = await CreateRepositoryQueryAsync();
            query.Configure(configureQuery);
            return await DoCountAsync(query);
        }

        public virtual async Task<TEntity?> GetByIdAsync(TEntityPk id)
        {
            var query = await CreateRepositoryQueryAsync();
            query.Where(i => i.Id.Equals(id));

            return await DoGetAsync(query);
        }

        public virtual async Task<TEntity?> GetByIdAsync(TEntityPk id,
            Func<IRepositoryQuery<TEntity>, Task> configureQuery)
        {
            var query = await CreateRepositoryQueryAsync();
            query.Where(i => i.Id.Equals(id));
            await query.ConfigureAsync(configureQuery);

            return await DoGetAsync(query);
        }

        public virtual async Task<TEntity?> GetByIdAsync(TEntityPk id, Action<IRepositoryQuery<TEntity>> configureQuery)
        {
            var query = await CreateRepositoryQueryAsync();
            query.Where(i => i.Id.Equals(id));
            query.Configure(configureQuery);

            return await DoGetAsync(query);
        }

        public virtual async Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids)
        {
            var query = await CreateRepositoryQueryAsync();
            query.Where(i => ids.Contains(i.Id));

            (TEntity[] items, _) = await DoGetAllAsync(query);

            return items;
        }

        public virtual async Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids,
            Func<IRepositoryQuery<TEntity>, Task> configureQuery)
        {
            var query = await CreateRepositoryQueryAsync();
            query.Where(i => ids.Contains(i.Id));
            await query.ConfigureAsync(configureQuery);

            (TEntity[] items, _) = await DoGetAllAsync(query);

            return items;
        }

        public virtual async Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids,
            Action<IRepositoryQuery<TEntity>> configureQuery)
        {
            var query = await CreateRepositoryQueryAsync();
            query.Where(i => ids.Contains(i.Id)).Configure(configureQuery);

            (TEntity[] items, _) = await DoGetAllAsync(query);

            return items;
        }

        protected virtual Task AfterLoadAsync(TEntity entity)
        {
            return entity != null ? AfterLoadAsync(new[] {entity}) : Task.CompletedTask;
        }

        protected virtual Task AfterLoadAsync(TEntity[] entities)
        {
            return Task.CompletedTask;
        }

        protected virtual Task<bool> BeforeSaveAsync(TEntity item,
            (bool isValid, IList<ValidationFailure> errors) validationResult, bool isNew,
            PropertyChange[] changes = null)
        {
            return FiltersManager.BeforeSaveAsync<TEntity, TEntityPk>(item, validationResult, isNew, changes);
        }

        protected virtual Task<bool> AfterSaveAsync(TEntity item, bool isNew, PropertyChange[] changes = null)
        {
            return FiltersManager.AfterSaveAsync<TEntity, TEntityPk>(item, isNew, changes);
        }

        protected virtual Task BeforeDeleteAsync(TEntity entity)
        {
            return Task.CompletedTask;
        }

        protected virtual Task CheckAccessAsync(TEntity entity)
        {
            return entity != null ? CheckAccessAsync(new[] {entity}) : Task.CompletedTask;
        }

        protected virtual async Task CheckAccessAsync(TEntity[] entities)
        {
            foreach (var accessChecker in AccessCheckers)
            {
                await accessChecker.CheckAccessAsync(entities);
            }
        }

        protected virtual async Task<(bool isValid, IList<ValidationFailure> errors)> ValidateAsync(TEntity entity,
            bool isNew,
            PropertyChange[] changes = null)
        {
            var failures = new List<ValidationFailure>();
            if (Validators != null)
            {
                foreach (var validator in Validators)
                {
                    var result = await validator.ValidateAsync(entity);
                    if (!result.IsValid)
                    {
                        failures.AddRange(result.Errors);
                    }
                }
            }

            return (!failures.Any(), failures);
        }

        protected virtual Task<bool> BeforeValidateAsync(TEntity item,
            (bool isValid, IList<ValidationFailure> errors) validationResult,
            bool isNew,
            PropertyChange[] changes = null)
        {
            return FiltersManager.BeforeValidateAsync<TEntity, TEntityPk>(item, validationResult, isNew, changes);
        }
    }
}
