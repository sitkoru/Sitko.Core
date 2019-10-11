using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
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

        protected virtual Task<IQueryable<TEntity>> GetBaseQueryAsync(
            QueryContext<TEntity, TEntityPk> queryContext = null)
        {
            return Task.FromResult(ApplyContext(DbContext.Set<TEntity>(), queryContext));
        }

        protected virtual IQueryable<TEntity> ApplyContext(IQueryable<TEntity> query,
            QueryContext<TEntity, TEntityPk> queryContext)
        {
            if (queryContext == null) return query;


            if (queryContext.OrderBy != null)
            {
                query = !queryContext.OrderByDescending
                    ? query.OrderBy(queryContext.OrderBy)
                    : query.OrderByDescending(queryContext.OrderBy);
            }

            /*var method = typeof(EF).GetType().GetMethod("Property").MakeGenericMethod(typeof(int));
            query = query.Where(e => (dynamic) method.Invoke(null, new object[] {e, "bla"}) > 1);*/
            if (queryContext.SortQueries.Any())
            {
                foreach (var sortQuery in queryContext.SortQueries)
                {
                    query = sortQuery.isDescending
                        ? query.OrderByDescending(e => EF.Property<TEntity>(e, sortQuery.propertyName))
                        : query.OrderBy(e => EF.Property<TEntity>(e, sortQuery.propertyName));
                }
            }

            if (queryContext.ConditionsGroups.Any())
            {
                //var method = GetType().GetMethod(nameof(AddWhereCondition));
                var where = new List<string>();
                var valueIndex = 0;
                var values = new List<object>();
                foreach (var conditionsGroup in queryContext.ConditionsGroups)
                {
                    var groupWhere = new List<string>();
                    foreach (var condition in conditionsGroup.Conditions)
                    {
                        var expression = condition.GetExpression(valueIndex);
                        if (!string.IsNullOrEmpty(expression))
                        {
                            groupWhere.Add(expression);
                            values.Add(condition.Value);
                            valueIndex++;
                        }
                    }

                    where.Add($"({string.Join(" OR ", groupWhere)})");
                }

                var whereStr = string.Join(" AND ", where);
                query = query.Where(whereStr, values.ToArray());
            }

            return query;
        }

        public virtual async Task<(TEntity[] items, int itemsCount)> GetAllAsync(
            QueryContext<TEntity, TEntityPk> queryContext = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>> addConditionsCallback = null)
        {
            var itemsCount = await CountAsync(queryContext, addConditionsCallback);

            var query = await GetBaseQueryAsync(queryContext);
            if (addConditionsCallback != null)
            {
                query = addConditionsCallback(query);
            }

            if (queryContext != null)
            {
                if (queryContext.Offset.HasValue)
                {
                    query = query.Skip(queryContext.Offset.Value);
                }

                if (queryContext.Limit.HasValue)
                {
                    query = query.Take(queryContext.Limit.Value);
                }
            }

            var items = await query.ToArrayAsync();
            await AfterLoadAsync(items);
            await CheckAccessAsync(items);

            return (items, itemsCount);
        }

        protected virtual Task AfterLoadAsync(TEntity entity)
        {
            return AfterLoadAsync(new[] {entity});
        }

        protected virtual Task AfterLoadAsync(TEntity[] entities)
        {
            return Task.CompletedTask;
        }

        public virtual async Task<int> CountAsync(QueryContext<TEntity, TEntityPk> queryContext = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>> addConditionsCallback = null)
        {
            var query = await GetBaseQueryAsync(queryContext);
            if (addConditionsCallback != null)
            {
                query = addConditionsCallback(query);
            }


            return await query.CountAsync();
        }

        public virtual async Task<TEntity> GetByIdAsync(TEntityPk id,
            QueryContext<TEntity, TEntityPk> queryContext = null)
        {
            var item = await (await GetBaseQueryAsync(queryContext)).FirstOrDefaultAsync(i => i.Id.Equals(id));
            await AfterLoadAsync(item);
            await CheckAccessAsync(item);
            return item;
        }

        public virtual async Task<TEntity> NewAsync()
        {
            var item = Activator.CreateInstance<TEntity>();
            await AfterLoadAsync(item);
            return item;
        }

        public virtual async Task<TEntity[]> GetByIdsAsync(TEntityPk[] ids,
            QueryContext<TEntity, TEntityPk> queryContext = null)
        {
            var items = await (await GetBaseQueryAsync(queryContext)).Where(i => ids.Contains(i.Id)).ToArrayAsync();
            await AfterLoadAsync(items);
            await CheckAccessAsync(items);

            return items;
        }

        public virtual async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddAsync(TEntity entity)
        {
            (bool isValid, IList<ValidationFailure> errors) validationResult = (false, new List<ValidationFailure>());
            if (await BeforeValidateAsync(entity, validationResult))
            {
                validationResult = await ValidateAsync(entity);
                if (validationResult.isValid)
                {
                    if (await BeforeSaveAsync(entity, validationResult))
                    {
                        DbContext.Add(entity);
                        await DbContext.SaveChangesAsync();
                        await AfterSaveAsync(entity);
                    }
                }
            }

            return new AddOrUpdateOperationResult<TEntity, TEntityPk>(entity, validationResult.errors);
        }

        public virtual async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateAsync(TEntity entity)
        {
            var changes = GetChanges(entity);
            (bool isValid, IList<ValidationFailure> errors) validationResult = (false, new List<ValidationFailure>());
            if (await BeforeValidateAsync(entity, validationResult, changes))
            {
                validationResult = await ValidateAsync(entity, changes);
                if (validationResult.isValid)
                {
                    if (await BeforeSaveAsync(entity, validationResult, changes))
                    {
                        DbContext.Update(entity);
                        await DbContext.SaveChangesAsync();
                        await AfterSaveAsync(entity, changes);
                    }
                }
            }

            return new AddOrUpdateOperationResult<TEntity, TEntityPk>(entity, validationResult.errors);
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


        public PropertyChange[] GetChanges(TEntity entity)
        {
            return (from propertyEntry in DbContext.Entry(entity).Properties
                where propertyEntry.IsModified
                let name = propertyEntry.Metadata.Name
                let originalValue = propertyEntry.OriginalValue
                let value = propertyEntry.CurrentValue
                select new PropertyChange(name, originalValue, value)).ToArray();
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
