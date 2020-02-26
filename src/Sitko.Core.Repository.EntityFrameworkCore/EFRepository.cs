using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    public abstract class EFRepository<TEntity, TEntityPk, TDbContext> :
        BaseRepository<TEntity, TEntityPk, EFRepositoryQuery<TEntity>>
        where TEntity : class, IEntity<TEntityPk> where TDbContext : DbContext
    {
        protected readonly TDbContext DbContext;
        private IDbContextTransaction? _transaction;

        protected EFRepository(EFRepositoryContext<TEntity, TEntityPk, TDbContext> repositoryContext) : base(
            repositoryContext)
        {
            DbContext = repositoryContext.DbContext;
        }

        protected override async Task<(TEntity[] items, bool needCount)> DoGetAllAsync(EFRepositoryQuery<TEntity> query)
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

        protected override async Task<TEntity?> DoGetAsync(EFRepositoryQuery<TEntity> query)
        {
            var dbQuery = query.BuildQuery();
            var item = await AddIncludes(dbQuery).FirstOrDefaultAsync();
            if (item != null)
            {
                await AfterLoadAsync(item);
            }

            return item;
        }

        protected override Task<int> DoCountAsync(EFRepositoryQuery<TEntity> query)
        {
            return query.BuildQuery().CountAsync();
        }

        protected override async Task DoSaveAsync()
        {
            await DbContext.SaveChangesAsync();
        }

        protected override Task DoAddAsync(TEntity item)
        {
            return DbContext.AddAsync(item).AsTask();
        }

        public override PropertyChange[] GetChanges(TEntity item, TEntity oldEntity)
        {
            var changes = new List<PropertyChange>();
            foreach (var propertyEntry in DbContext.Entry(item).Properties)
            {
                var name = propertyEntry.Metadata.Name;
                if (propertyEntry.IsModified)
                {
                    var originalValue = propertyEntry.OriginalValue;
                    var value = propertyEntry.CurrentValue;
                    changes.Add(new PropertyChange(name, originalValue, value));
                }
                else
                {
                    var property = item.GetType().GetProperty(propertyEntry.Metadata.Name);
                    if (property != null)
                    {
                        var value = property.GetValue(item);
                        var originalValue = property.GetValue(oldEntity);
                        if (value == null && originalValue != null || value != null && !value.Equals(originalValue))
                        {
                            propertyEntry.IsModified = true;
                            changes.Add(new PropertyChange(name, originalValue, value));
                        }
                    }
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

        protected override Task<TEntity> GetOldItem(TEntityPk id)
        {
            return GetBaseQuery().Where(e => e.Id.Equals(id)).AsNoTracking().FirstAsync();
        }

        protected override Task DoUpdateAsync(TEntity item)
        {
            return Task.CompletedTask;
        }

        protected override Task DoDeleteAsync(TEntity item)
        {
            DbContext.Remove(item);
            return Task.CompletedTask;
        }


        protected IQueryable<TEntity> GetBaseQuery()
        {
            return DbContext.Set<TEntity>().AsQueryable();
        }

        protected virtual IQueryable<TEntity> AddIncludes(IQueryable<TEntity> query)
        {
            return query;
        }

        protected override Task<EFRepositoryQuery<TEntity>> CreateRepositoryQueryAsync()
        {
            return Task.FromResult(new EFRepositoryQuery<TEntity>(GetBaseQuery()));
        }

        public override async Task<bool> BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                return true;
            }

            try
            {
                _transaction = DbContext.Database.CurrentTransaction ??
                               await DbContext.Database.BeginTransactionAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while starting transaction in {Repository}: {ErrorText}", GetType(),
                    ex.ToString());
                return false;
            }

            return true;
        }

        public override async Task<bool> CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                try
                {
                    await _transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while commiting transaction {Id} in {Repository}: {ErrorText}",
                        _transaction.TransactionId, GetType(),
                        ex.ToString());
                    return false;
                }
            }

            return false;
        }

        public override async Task<bool> RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                try
                {
                    await _transaction.RollbackAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while rollback transaction {Id} in {Repository}: {ErrorText}",
                        _transaction.TransactionId, GetType(),
                        ex.ToString());
                    return false;
                }
            }

            return false;
        }
    }
}
