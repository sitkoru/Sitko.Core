using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    using System.Linq.Expressions;

    public abstract class EFRepository<TEntity, TEntityPk, TDbContext> :
        BaseRepository<TEntity, TEntityPk, EFRepositoryQuery<TEntity>>
        where TEntity : class, IEntity<TEntityPk> where TDbContext : DbContext
    {
        private readonly AsyncLock asyncLock = new();
        private readonly TDbContext dbContext;
        private readonly EFRepositoryContext<TEntity, TEntityPk, TDbContext> repositoryContext;

        protected EFRepository(EFRepositoryContext<TEntity, TEntityPk, TDbContext> repositoryContext) : base(
            repositoryContext)
        {
            this.repositoryContext = repositoryContext;
            dbContext = repositoryContext.DbContext;
        }

        protected async Task<T> ExecuteDbContextOperationAsync<T>(Func<TDbContext, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            using (await asyncLock.LockAsync(cancellationToken))
            {
                return await operation(dbContext);
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

            var result = await ExecuteDbContextOperationAsync(_ => AddIncludes(dbQuery).ToArrayAsync(cancellationToken),
                cancellationToken);

            return (result, needCount);
        }


        protected override async Task<TEntity?> DoGetAsync(EFRepositoryQuery<TEntity> query,
            CancellationToken cancellationToken = default)
        {
            var dbQuery = query.BuildQuery();
            var item = await ExecuteDbContextOperationAsync(
                _ => AddIncludes(dbQuery).FirstOrDefaultAsync(cancellationToken), cancellationToken);
            if (item != null)
            {
                await AfterLoadAsync(item, cancellationToken);
            }

            return item;
        }

        protected override Task<int> DoSumAsync(EFRepositoryQuery<TEntity> query,
            Expression<Func<TEntity, int>> selector,
            CancellationToken cancellationToken = default) =>
            ExecuteDbContextOperationAsync(_ => query.BuildQuery().SumAsync(selector, cancellationToken),
                cancellationToken);

        protected override Task<long> DoSumAsync(EFRepositoryQuery<TEntity> query,
            Expression<Func<TEntity, long>> selector,
            CancellationToken cancellationToken = default) =>
            ExecuteDbContextOperationAsync(_ => query.BuildQuery().SumAsync(selector, cancellationToken),
                cancellationToken);

        protected override Task<double> DoSumAsync(EFRepositoryQuery<TEntity> query,
            Expression<Func<TEntity, double>> selector,
            CancellationToken cancellationToken = default) =>
            ExecuteDbContextOperationAsync(_ => query.BuildQuery().SumAsync(selector, cancellationToken),
                cancellationToken);

        protected override Task<decimal> DoSumAsync(EFRepositoryQuery<TEntity> query,
            Expression<Func<TEntity, decimal>> selector,
            CancellationToken cancellationToken = default) =>
            ExecuteDbContextOperationAsync(_ => query.BuildQuery().SumAsync(selector, cancellationToken),
                cancellationToken);

        protected override Task<float> DoSumAsync(EFRepositoryQuery<TEntity> query,
            Expression<Func<TEntity, float>> selector,
            CancellationToken cancellationToken = default) =>
            ExecuteDbContextOperationAsync(_ => query.BuildQuery().SumAsync(selector, cancellationToken),
                cancellationToken);

        protected override Task<int?> DoSumAsync(EFRepositoryQuery<TEntity> query,
            Expression<Func<TEntity, int?>> selector,
            CancellationToken cancellationToken = default) =>
            ExecuteDbContextOperationAsync(_ => query.BuildQuery().SumAsync(selector, cancellationToken),
                cancellationToken);

        protected override Task<long?> DoSumAsync(EFRepositoryQuery<TEntity> query,
            Expression<Func<TEntity, long?>> selector,
            CancellationToken cancellationToken = default) =>
            ExecuteDbContextOperationAsync(_ => query.BuildQuery().SumAsync(selector, cancellationToken),
                cancellationToken);

        protected override Task<double?> DoSumAsync(EFRepositoryQuery<TEntity> query,
            Expression<Func<TEntity, double?>> selector,
            CancellationToken cancellationToken = default) =>
            ExecuteDbContextOperationAsync(_ => query.BuildQuery().SumAsync(selector, cancellationToken),
                cancellationToken);

        protected override Task<decimal?> DoSumAsync(EFRepositoryQuery<TEntity> query,
            Expression<Func<TEntity, decimal?>> selector,
            CancellationToken cancellationToken = default) =>
            ExecuteDbContextOperationAsync(_ => query.BuildQuery().SumAsync(selector, cancellationToken),
                cancellationToken);

        protected override Task<float?> DoSumAsync(EFRepositoryQuery<TEntity> query,
            Expression<Func<TEntity, float?>> selector,
            CancellationToken cancellationToken = default) =>
            ExecuteDbContextOperationAsync(_ => query.BuildQuery().SumAsync(selector, cancellationToken),
                cancellationToken);


        protected override Task<int> DoCountAsync(EFRepositoryQuery<TEntity> query,
            CancellationToken cancellationToken = default) =>
            ExecuteDbContextOperationAsync(_ => query.BuildQuery().CountAsync(cancellationToken),
                cancellationToken);

        protected override async Task DoSaveAsync(CancellationToken cancellationToken = default) =>
            await ExecuteDbContextOperationAsync(
                currentDbContext => currentDbContext.SaveChangesAsync(cancellationToken),
                cancellationToken);

        protected override async Task DoAddAsync(TEntity item, CancellationToken cancellationToken = default) =>
            await ExecuteDbContextOperationAsync(
                currentDbContext => currentDbContext.AddAsync(item, cancellationToken).AsTask(),
                cancellationToken);

        protected override async Task<(PropertyChange[] changes, TEntity oldEntity)> GetChangesAsync(TEntity item)
        {
            using var scope = repositoryContext.CreateScope();
            var oldDbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
            var oldEntity = await oldDbContext.Set<TEntity>().FirstOrDefaultAsync(e => e.Id!.Equals(item.Id));
            var entry = dbContext.Entry(item);
            var entityChanges = entry
                .Properties
                .Where(p => p.IsModified)
                .Select(p => new PropertyChange(p.Metadata.Name, p.OriginalValue, p.CurrentValue))
                .ToList();
            if (!entityChanges.Any() && dbContext.ChangeTracker.HasChanges())
            {
                foreach (var collection in entry.Collections)
                {
                    var oldCollection = oldDbContext.Entry(oldEntity).Collections
                        .First(c => c.Metadata.Name == collection.Metadata.Name);
                    await oldCollection.LoadAsync();
                    if (oldCollection.CurrentValue.Cast<object>().Count() !=
                        collection.CurrentValue.Cast<object>().Count())
                    {
                        entityChanges.Add(new PropertyChange(collection.Metadata.Name, oldCollection, collection));
                        continue;
                    }

                    if (collection.CurrentValue.Cast<object>().Any(collectionEntry =>
                        dbContext.Entry(collectionEntry).State != EntityState.Unchanged))
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

            return (entityChanges.ToArray(), oldEntity);
        }

        public DbSet<T> Set<T>() where T : class => dbContext.Set<T>();

        protected override Task DoUpdateAsync(TEntity item, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        protected override Task DoDeleteAsync(TEntity item, CancellationToken cancellationToken = default) =>
            ExecuteDbContextOperationAsync(currentDbContext =>
            {
                currentDbContext.Remove(item);
                return Task.FromResult(true);
            }, cancellationToken);


        protected IQueryable<TEntity> GetBaseQuery() => dbContext.Set<TEntity>().AsQueryable();

        protected virtual IQueryable<TEntity> AddIncludes(IQueryable<TEntity> query) => query;

        protected override Task<EFRepositoryQuery<TEntity>> CreateRepositoryQueryAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EFRepositoryQuery<TEntity>(GetBaseQuery()));

        public override async Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (GetCurrentTransaction() != null)
            {
                return true;
            }

            try
            {
                await ExecuteDbContextOperationAsync(async currentDbContext =>
                    currentDbContext.Database.CurrentTransaction ??
                    await currentDbContext.Database
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

        private IDbContextTransaction? GetCurrentTransaction() => dbContext.Database.CurrentTransaction;

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

        public override Task RefreshAsync(TEntity entity, CancellationToken cancellationToken = default) =>
            dbContext.Entry(entity).ReloadAsync(cancellationToken);
    }
}
