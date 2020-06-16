using System;
using System.Linq;
using System.Threading;
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
        private readonly TDbContext _dbContext;
        private readonly EFRepositoryLock? _lock;

        protected EFRepository(EFRepositoryContext<TEntity, TEntityPk, TDbContext> repositoryContext) : base(
            repositoryContext)
        {
            _dbContext = repositoryContext.DbContext;
            _lock = repositoryContext.RepositoryLock;
        }

        protected async Task<T> ExecuteDbContextOperationAsync<T>(Func<TDbContext, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            if (_lock == null)
            {
                return await operation(_dbContext);
            }

            await _lock.WaitAsync(cancellationToken);
            try
            {
                var result = await operation(_dbContext);
                _lock.Release();
                return result;
            }
            catch (Exception)
            {
                _lock.Release();
                throw;
            }
        }

        protected async Task ExecuteDbContextOperationAsync(Func<TDbContext, Task> operation,
            CancellationToken cancellationToken = default)
        {
            if (_lock == null)
            {
                await operation(_dbContext);
                return;
            }

            await _lock.WaitAsync(cancellationToken);
            try
            {
                await operation(_dbContext);
                _lock.Release();
            }
            catch (Exception)
            {
                _lock.Release();
                throw;
            }
        }


        protected async Task<T> ExecuteDbContextOperationAsync<T>(Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            if (_lock == null)
            {
                return await operation();
            }

            await _lock.WaitAsync(cancellationToken);
            try
            {
                var result = await operation();
                _lock.Release();
                return result;
            }
            catch (Exception)
            {
                _lock.Release();
                throw;
            }
        }

        protected async Task ExecuteDbContextOperationAsync(Func<Task> operation,
            CancellationToken cancellationToken = default)
        {
            if (_lock == null)
            {
                await operation();
                return;
            }

            await _lock.WaitAsync(cancellationToken);
            try
            {
                await operation();
                _lock.Release();
            }
            catch (Exception)
            {
                _lock.Release();
                throw;
            }
        }

        protected T ExecuteDbContextOperation<T>(Func<TDbContext, T> operation,
            CancellationToken cancellationToken = default)
        {
            if (_lock == null)
            {
                return operation(_dbContext);
            }

            _lock.Wait(cancellationToken);
            try
            {
                var result = operation(_dbContext);
                _lock.Release();
                return result;
            }
            catch (Exception)
            {
                _lock.Release();
                throw;
            }
        }

        protected void ExecuteDbContextOperation(Action<TDbContext> operation,
            CancellationToken cancellationToken = default)
        {
            if (_lock == null)
            {
                operation(_dbContext);
                return;
            }

            _lock.Wait(cancellationToken);
            try
            {
                operation(_dbContext);
                _lock.Release();
            }
            catch (Exception)
            {
                _lock.Release();
                throw;
            }
        }

        protected override async Task<(TEntity[] items, bool needCount)> DoGetAllAsync(EFRepositoryQuery<TEntity> query,
            CancellationToken cancellationToken = default)
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

            var result = await ExecuteDbContextOperationAsync(
                () => AddIncludes(dbQuery).ToArrayAsync(cancellationToken),
                cancellationToken);

            return (result, needCount);
        }

        protected override async Task<TEntity?> DoGetAsync(EFRepositoryQuery<TEntity> query,
            CancellationToken cancellationToken = default)
        {
            var dbQuery = query.BuildQuery();
            var item = await ExecuteDbContextOperationAsync(
                () => AddIncludes(dbQuery).FirstOrDefaultAsync(cancellationToken), cancellationToken);
            if (item != null)
            {
                await AfterLoadAsync(item, cancellationToken);
            }

            return item;
        }

        protected override Task<int> DoCountAsync(EFRepositoryQuery<TEntity> query,
            CancellationToken cancellationToken = default)
        {
            return ExecuteDbContextOperationAsync(() => query.BuildQuery().CountAsync(cancellationToken),
                cancellationToken);
        }

        protected override async Task DoSaveAsync(CancellationToken cancellationToken = default)
        {
            await ExecuteDbContextOperationAsync(dbContext => dbContext.SaveChangesAsync(cancellationToken),
                cancellationToken);
        }

        protected override async Task DoAddAsync(TEntity item, CancellationToken cancellationToken = default)
        {
            await ExecuteDbContextOperationAsync(dbContext => dbContext.AddAsync(item, cancellationToken).AsTask(),
                cancellationToken);
        }

        public override PropertyChange[] GetChanges(TEntity item, TEntity oldEntity)
        {
            var entry = _dbContext.Entry(item);
            var entityChanges = entry
                .Properties
                .Where(p => p.IsModified)
                .Select(p => new PropertyChange(p.Metadata.Name, p.OriginalValue, p.CurrentValue))
                .ToList();
            if (!entityChanges.Any() && _dbContext.ChangeTracker.HasChanges())
            {
                foreach (var collection in entry.Collections)
                {
                    if (collection.CurrentValue.Cast<object>().Any(collectionEntry =>
                        _dbContext.Entry(collectionEntry).State == EntityState.Modified))
                    {
                        entityChanges.Add(new PropertyChange(collection.Metadata.Name, collection, collection));
                    }
                }

                foreach (var reference in entry.References)
                {
                    if (reference.IsModified)
                    {
                        entityChanges.Add(new PropertyChange(reference.Metadata.Name, reference.CurrentValue,
                            reference.CurrentValue));
                    }
                }
            }

            return entityChanges.ToArray();
        }

        public DbSet<T> Set<T>() where T : class
        {
            return _dbContext.Set<T>();
        }

        protected override Task<TEntity> GetOldItemAsync(TEntityPk id, CancellationToken cancellationToken = default)
        {
            return ExecuteDbContextOperationAsync(_ =>
                    GetBaseQuery().Where(e => e.Id!.Equals(id)).AsNoTracking().FirstAsync(cancellationToken),
                cancellationToken);
        }

        protected override Task DoUpdateAsync(TEntity item, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        protected override Task DoDeleteAsync(TEntity item, CancellationToken cancellationToken = default)
        {
            ExecuteDbContextOperation(dbContext => dbContext.Remove(item));
            return Task.CompletedTask;
        }


        protected IQueryable<TEntity> GetBaseQuery()
        {
            return _dbContext.Set<TEntity>().AsQueryable();
        }

        protected virtual IQueryable<TEntity> AddIncludes(IQueryable<TEntity> query)
        {
            return query;
        }

        protected override Task<EFRepositoryQuery<TEntity>> CreateRepositoryQueryAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EFRepositoryQuery<TEntity>(GetBaseQuery()));
        }

        public override async Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (GetCurrentTransaction() != null)
            {
                return true;
            }

            try
            {
                await ExecuteDbContextOperationAsync(async dbContext =>
                    dbContext.Database.CurrentTransaction ??
                    await dbContext.Database
                        .BeginTransactionAsync(
                            cancellationToken), cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while starting transaction in {Repository}: {ErrorText}", GetType(),
                    ex.ToString());
                return false;
            }

            return true;
        }

        private IDbContextTransaction? GetCurrentTransaction()
        {
            return _dbContext.Database.CurrentTransaction;
        }

        public override async Task<bool> CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = GetCurrentTransaction();
            if (transaction != null)
            {
                try
                {
                    await transaction.CommitAsync(cancellationToken);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while commiting transaction {Id} in {Repository}: {ErrorText}",
                        transaction.TransactionId, GetType(),
                        ex.ToString());
                    return false;
                }
            }

            return false;
        }

        public override async Task<bool> RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = GetCurrentTransaction();
            if (transaction != null)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error while rollback transaction {Id} in {Repository}: {ErrorText}",
                        transaction.TransactionId, GetType(),
                        ex.ToString());
                    return false;
                }
            }

            return false;
        }
    }
}
