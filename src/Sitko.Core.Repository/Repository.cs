using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Repository
{
    public abstract class Repository<TEntity, TEntityPk, TDbContext> : IRepository<TEntity, TEntityPk>
        where TEntity : class, IEntity<TEntityPk> where TDbContext : DbContext
    {
        internal readonly TDbContext DbContext;
        protected readonly List<IValidator<TEntity>> Validators;
        protected readonly List<IRepositoryFilter> Filters;
        protected readonly List<IAccessChecker<TEntity, TEntityPk>> AccessCheckers;
        protected readonly ILogger Logger;

        protected Repository(RepositoryContext<TEntity, TEntityPk, TDbContext> repositoryContext)
        {
            DbContext = repositoryContext.DbContext;
            Validators = repositoryContext.Validators ?? new List<IValidator<TEntity>>();
            Filters = repositoryContext.Filters ?? new List<IRepositoryFilter>();
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

        public virtual async Task<(TEntity[] items, int itemsCount)> GetAllAsync()
        {
            var query = CreateRepositoryQuery();

            (TEntity[] items, bool needCount) = await DoGetAllAsync(query);

            var itemsCount = needCount && (query.Offset > 0 || items.Length == query.Limit)
                ? await CountAsync()
                : items.Length;
            await AfterLoadAsync(items);

            return (items, itemsCount);
        }

        public virtual async Task<(TEntity[] items, int itemsCount)> GetAllAsync(
            Action<RepositoryQuery<TEntity>> configureQuery)
        {
            var query = CreateRepositoryQuery().Configure(configureQuery);

            (TEntity[] items, bool needCount) = await DoGetAllAsync(query);

            var itemsCount = needCount && (query.Offset > 0 || items.Length == query.Limit)
                ? await CountAsync(configureQuery)
                : items.Length;
            await AfterLoadAsync(items);

            return (items, itemsCount);
        }

        protected virtual async Task<(TEntity[] items, bool needCount)> DoGetAllAsync(RepositoryQuery<TEntity> query)
        {
            var dbQuery = query.BuildQuery();
            var needCount = false;
            if (query.Offset != null)
            {
                dbQuery = dbQuery.Skip(query.Offset.Value);
                needCount = true;
            }

            if (query.Limit != null)
            {
                dbQuery = dbQuery.Take(query.Limit.Value);
                needCount = true;
            }

            return (await AddIncludes(dbQuery).ToArrayAsync(), needCount);
        }

        public virtual async Task<(TEntity[] items, int itemsCount)> GetAllAsync(
            Func<RepositoryQuery<TEntity>, Task> configureQuery)
        {
            var query = await CreateRepositoryQuery().ConfigureAsync(configureQuery);
            var (items, needCount) = await DoGetAllAsync(query);

            var itemsCount = needCount && (query.Offset > 0 || items.Length == query.Limit)
                ? await CountAsync(configureQuery)
                : items.Length;
            await AfterLoadAsync(items);

            return (items, itemsCount);
        }

        protected virtual Task AfterLoadAsync(TEntity entity)
        {
            return entity != null ? AfterLoadAsync(new[] {entity}) : Task.CompletedTask;
        }

        protected virtual Task AfterLoadAsync(TEntity[] entities)
        {
            return Task.CompletedTask;
        }

        public virtual async Task<int> CountAsync()
        {
            return await CreateRepositoryQuery().BuildQuery().CountAsync();
        }

        public virtual async Task<int> CountAsync(Func<RepositoryQuery<TEntity>, Task> configureQuery)
        {
            return await (await CreateRepositoryQuery().ConfigureAsync(configureQuery)).BuildQuery().CountAsync();
        }

        public virtual async Task<int> CountAsync(Action<RepositoryQuery<TEntity>> configureQuery)
        {
            return await CreateRepositoryQuery().Configure(configureQuery).BuildQuery().CountAsync();
        }

        public virtual async Task<TEntity?> GetByIdAsync(TEntityPk id)
        {
            var query = CreateRepositoryQuery().Where(i => i.Id.Equals(id)).BuildQuery();

            return await DoGetAsync(query);
        }

        public virtual async Task<TEntity?> GetByIdAsync(TEntityPk id,
            Func<RepositoryQuery<TEntity>, Task> configureQuery)
        {
            var query = (await CreateRepositoryQuery().Where(i => i.Id.Equals(id)).ConfigureAsync(configureQuery))
                .BuildQuery();

            return await DoGetAsync(query);
        }

        public virtual async Task<TEntity?> GetByIdAsync(TEntityPk id, Action<RepositoryQuery<TEntity>> configureQuery)
        {
            var query = CreateRepositoryQuery().Where(i => i.Id.Equals(id)).Configure(configureQuery)
                .BuildQuery();

            return await DoGetAsync(query);
        }

        public virtual Task<TEntity?> GetAsync()
        {
            var query = CreateRepositoryQuery().BuildQuery();
            return DoGetAsync(query);
        }

        public virtual async Task<TEntity?> GetAsync(Func<RepositoryQuery<TEntity>, Task> configureQuery)
        {
            var query = (await CreateRepositoryQuery().ConfigureAsync(configureQuery)).BuildQuery();
            return await DoGetAsync(query);
        }

        public virtual Task<TEntity?> GetAsync(Action<RepositoryQuery<TEntity>> configureQuery)
        {
            var query = CreateRepositoryQuery().Configure(configureQuery).BuildQuery();
            return DoGetAsync(query);
        }

        private async Task<TEntity?> DoGetAsync(IQueryable<TEntity> query)
        {
            var item = await AddIncludes(query).FirstOrDefaultAsync();
            if (item != null)
            {
                await AfterLoadAsync(item);
            }

            return item;
        }

        public virtual async Task<TEntity> NewAsync()
        {
            var item = Activator.CreateInstance<TEntity>();
            await AfterLoadAsync(item);
            return item;
        }

        public virtual async Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids)
        {
            var query = CreateRepositoryQuery().Where(i => ids.Contains(i.Id));

            (TEntity[] items, _) = await DoGetAllAsync(query);

            return items;
        }

        public virtual async Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids,
            Func<RepositoryQuery<TEntity>, Task> configureQuery)
        {
            var query = await CreateRepositoryQuery().Where(i => ids.Contains(i.Id)).ConfigureAsync(configureQuery);

            (TEntity[] items, _) = await DoGetAllAsync(query);

            return items;
        }

        public virtual async Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids,
            Action<RepositoryQuery<TEntity>> configureQuery)
        {
            var query = CreateRepositoryQuery().Where(i => ids.Contains(i.Id)).Configure(configureQuery);

            (TEntity[] items, _) = await DoGetAllAsync(query);

            return items;
        }

        public virtual async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddAsync(TEntity item)
        {
            var validationResult = await DoAddAsync(item);
            if (validationResult.isValid)
            {
                await DoSaveAsync(item);
            }

            return new AddOrUpdateOperationResult<TEntity, TEntityPk>(item, validationResult.errors,
                new PropertyChange[0]);
        }

        protected async Task DoSaveAsync(TEntity item, PropertyChange[]? changes = null,
            TEntity? oldItem = null)
        {
            await SaveChangesAsync();
            await AfterSaveAsync(item);
        }

        protected virtual async Task SaveChangesAsync()
        {
            await DbContext.SaveChangesAsync();
        }

        protected async Task<(bool isValid, IList<ValidationFailure> errors)> DoAddAsync(TEntity item)
        {
            (bool isValid, IList<ValidationFailure> errors) validationResult = (false, new List<ValidationFailure>());
            if (await BeforeValidateAsync(item, validationResult))
            {
                validationResult = await ValidateAsync(item);
                if (validationResult.isValid)
                {
                    if (await BeforeSaveAsync(item, validationResult))
                    {
                        DbContext.Add(item);
                    }
                }
            }

            return validationResult;
        }

        public PropertyChange[] GetChanges(TEntity item, TEntity oldEntity)
        {
            var changes = new List<PropertyChange>();
            foreach (var propertyEntry in DbContext.Entry(item).Properties)
            {
                if (propertyEntry.IsModified)
                {
                    var name = propertyEntry.Metadata.Name;
                    var originalValue = propertyEntry.OriginalValue;
                    var value = propertyEntry.CurrentValue;
                    changes.Add(new PropertyChange(name, originalValue, value));
                }
            }

            foreach (var navigationEntry in DbContext.Entry(item).Navigations)
            {
                var property = item.GetType().GetProperty(navigationEntry.Metadata.Name);
                if (property != null)
                {
                    var value = property.GetValue(item);
                    var originalValue = property.GetValue(oldEntity);
                    if (value == null && originalValue != null || value != null && !value.Equals(originalValue))
                    {
                        var name = navigationEntry.Metadata.Name;
                        changes.Add(new PropertyChange(name, originalValue, value));
                    }
                }
            }

            return changes.ToArray();
        }

        public DbSet<T> Set<T>() where T : class
        {
            return DbContext.Set<T>();
        }

        public virtual async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync(TEntity item)
        {
            var (validationResult, changes, oldItem) = await DoUpdateAsync(item);
            if (validationResult.isValid)
            {
                await DoSaveAsync(item, changes, oldItem);
            }

            return new AddOrUpdateOperationResult<TEntity, TEntityPk>(item, validationResult.errors, changes);
        }

        protected async
            Task<((bool isValid, IList<ValidationFailure> errors) validationResult, PropertyChange[] changes, TEntity
                oldItem)> DoUpdateAsync(TEntity item)
        {
            var oldItem = GetBaseQuery().Where(e => e.Id.Equals(item.Id)).AsNoTracking().First();
            var changes = GetChanges(item, oldItem);
            (bool isValid, IList<ValidationFailure> errors) validationResult = (false, new List<ValidationFailure>());
            if (await BeforeValidateAsync(item, validationResult, changes))
            {
                validationResult = await ValidateAsync(item, changes);
                if (validationResult.isValid)
                {
                    if (await BeforeSaveAsync(item, validationResult, changes))
                    {
                        DbContext.Update(item);
                    }
                }
            }

            return (validationResult, changes, oldItem);
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
                DbContext.Remove(entity);
                await DbContext.SaveChangesAsync();
                return true;
            }

            return false;
        }


        protected virtual async Task<(bool isValid, IList<ValidationFailure> errors)> ValidateAsync(TEntity entity,
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

        protected virtual async Task<bool> BeforeValidateAsync(TEntity item,
            (bool isValid, IList<ValidationFailure> errors) validationResult,
            PropertyChange[] changes = null)
        {
            var result = true;
            foreach (var repositoryFilter in Filters)
            {
                if (!repositoryFilter.CanProcess(item.GetType())) continue;
                if (!await repositoryFilter.BeforeValidateAsync<TEntity, TEntityPk>(item, validationResult, changes))
                {
                    result = false;
                }
            }

            return result;
        }

        public IQueryable<TEntity> GetBaseQuery()
        {
            return DbContext.Set<TEntity>().AsQueryable();
        }

        protected virtual IQueryable<TEntity> AddIncludes(IQueryable<TEntity> query)
        {
            return query;
        }

        public virtual RepositoryQuery<TEntity> CreateRepositoryQuery()
        {
            return new RepositoryQuery<TEntity>(GetBaseQuery());
        }

        protected virtual async Task<bool> BeforeSaveAsync(TEntity item,
            (bool isValid, IList<ValidationFailure> errors) validationResult,
            PropertyChange[] changes = null)
        {
            var result = true;
            foreach (var repositoryFilter in Filters)
            {
                if (!repositoryFilter.CanProcess(item.GetType())) continue;
                if (!await repositoryFilter.BeforeSaveAsync<TEntity, TEntityPk>(item, validationResult, changes))
                {
                    result = false;
                }
            }

            return result;
        }

        protected virtual async Task<bool> AfterSaveAsync(TEntity item, PropertyChange[] changes = null)
        {
            var result = true;
            foreach (var repositoryFilter in Filters)
            {
                if (!repositoryFilter.CanProcess(item.GetType())) continue;
                if (!await repositoryFilter.AfterSaveAsync<TEntity, TEntityPk>(item, changes))
                {
                    result = false;
                }
            }

            return result;
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
    }
}
