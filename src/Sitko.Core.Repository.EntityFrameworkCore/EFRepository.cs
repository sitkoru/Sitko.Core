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
        protected readonly TDbContext DbContext;
        private IDbContextTransaction? _transaction;

        protected EFRepository(EFRepositoryContext<TEntity, TEntityPk, TDbContext> repositoryContext) : base(
            repositoryContext)
        {
            DbContext = repositoryContext.DbContext;
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

            return (await AddIncludes(dbQuery).ToArrayAsync(cancellationToken), needCount);
        }

        protected override async Task<TEntity?> DoGetAsync(EFRepositoryQuery<TEntity> query,
            CancellationToken cancellationToken = default)
        {
            var dbQuery = query.BuildQuery();
            var item = await AddIncludes(dbQuery).FirstOrDefaultAsync(cancellationToken);
            if (item != null)
            {
                await AfterLoadAsync(item, cancellationToken);
            }

            return item;
        }

        protected override Task<int> DoCountAsync(EFRepositoryQuery<TEntity> query,
            CancellationToken cancellationToken = default)
        {
            return query.BuildQuery().CountAsync(cancellationToken);
        }

        protected override async Task DoSaveAsync(CancellationToken cancellationToken = default)
        {
            await DbContext.SaveChangesAsync(cancellationToken);
        }

        protected override Task DoAddAsync(TEntity item, CancellationToken cancellationToken = default)
        {
            return DbContext.AddAsync(item, cancellationToken).AsTask();
        }

        public override PropertyChange[] GetChanges(TEntity item, TEntity oldEntity)
        {
            return DbContext
                .Entry(item)
                .Properties
                .Where(p => p.IsModified)
                .Select(p => new PropertyChange(p.Metadata.Name, p.OriginalValue, p.CurrentValue))
                .ToArray();
        }

        public DbSet<T> Set<T>() where T : class
        {
            return DbContext.Set<T>();
        }

        protected override Task<TEntity> GetOldItemAsync(TEntityPk id, CancellationToken cancellationToken = default)
        {
            return GetBaseQuery().Where(e => e.Id!.Equals(id)).AsNoTracking().FirstAsync(cancellationToken);
        }

        protected override Task DoUpdateAsync(TEntity item, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        protected override Task DoDeleteAsync(TEntity item, CancellationToken cancellationToken = default)
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

        protected override Task<EFRepositoryQuery<TEntity>> CreateRepositoryQueryAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EFRepositoryQuery<TEntity>(GetBaseQuery()));
        }

        public override async Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                return true;
            }

            try
            {
                _transaction = DbContext.Database.CurrentTransaction ??
                               await DbContext.Database.BeginTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while starting transaction in {Repository}: {ErrorText}", GetType(),
                    ex.ToString());
                return false;
            }

            return true;
        }

        public override async Task<bool> CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                try
                {
                    await _transaction.CommitAsync(cancellationToken);
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

        public override async Task<bool> RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                try
                {
                    await _transaction.RollbackAsync(cancellationToken);
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
