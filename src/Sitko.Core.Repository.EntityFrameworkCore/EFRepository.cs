using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;

namespace Sitko.Core.Repository.EntityFrameworkCore
{
    public abstract class EFRepository<TEntity, TEntityPk, TDbContext> :
        BaseRepository<TEntity, TEntityPk, EFRepositoryQuery<TEntity>>, IExternalRepository<TEntity, TEntityPk>
        where TEntity : class, IEntity<TEntityPk> where TDbContext : DbContext
    {
        private readonly TDbContext dbContext;
        private readonly EFRepositoryLock repositoryLock;

        private MethodInfo? updateCollectionMethodInfo;

        protected EFRepository(EFRepositoryContext<TEntity, TEntityPk, TDbContext> repositoryContext) : base(
            repositoryContext)
        {
            dbContext = repositoryContext.DbContext;
            repositoryLock = repositoryContext.RepositoryLock;
        }

        public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateExternalAsync(TEntity entity,
            TEntity? baseEntity = null,
            CancellationToken cancellationToken = default)
        {
            await AttachAsync(entity, baseEntity, cancellationToken);
            return await UpdateAsync(entity, cancellationToken);
        }

        public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateExternalAsync(TEntity entity,
            Action<TEntity> update,
            CancellationToken cancellationToken = default)
        {
            await AttachAsync(entity, null, cancellationToken);
            update(entity);
            return await UpdateAsync(entity, cancellationToken);
        }

        public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateExternalAsync(TEntity entity,
            Func<TEntity, Task> update,
            CancellationToken cancellationToken = default)
        {
            await AttachAsync(entity, null, cancellationToken);
            await update(entity);
            return await UpdateAsync(entity, cancellationToken);
        }

        public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddExternalAsync(TEntity entity,
            CancellationToken cancellationToken = default)
        {
            await AttachAsync(entity, null, cancellationToken);
            return await AddAsync(entity, cancellationToken);
        }

        public async Task<bool> DeleteExternalAsync(TEntity entity,
            CancellationToken cancellationToken = default)
        {
            await AttachAsync(entity, null, cancellationToken);
            return await DeleteAsync(entity, cancellationToken);
        }

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

        protected async Task AttachAsync(TEntity entity, TEntity? baseEntity = null,
            CancellationToken cancellationToken = default) =>
            await ExecuteDbContextOperationAsync(context =>
            {
                var loadedReferences =
                    new List<EntityReference>();
                if (baseEntity is not null)
                {
                    var baseEntry = dbContext.Entry<IEntity>(baseEntity);
                    AnalyzeReferences(baseEntry, loadedReferences);
                }

                context.ChangeTracker.TrackGraph(entity, node =>
                {
                    var nodeEntity = node.Entry.Entity as IEntity;
                    if (nodeEntity is not null)
                    {
                        var existedEntry = GetTrackedEntity(context, nodeEntity);
                        if (existedEntry is not null)
                        {
                            node.Entry.State = EntityState.Detached;
                            return;
                        }
                    }

                    var properties = node.Entry.GetDatabaseValues();
                    if (properties is null)
                    {
                        node.Entry.State = EntityState.Added;
                        return;
                    }

                    node.Entry.State = EntityState.Unchanged;
                    foreach (var property in node.Entry.Properties)
                    {
                        if (property.Metadata.IsKey() || property.Metadata.IsShadowProperty())
                        {
                            continue;
                        }

                        if (properties.TryGetValue<object>(property.Metadata.Name, out var value))
                        {
                            if (!property.Metadata.GetValueComparer().Equals(property.CurrentValue, value))
                            {
                                property.IsModified = true;
                                property.OriginalValue = value;
                            }
                        }
                    }


                    if (nodeEntity is not null)
                    {
                        var entryReferences = loadedReferences.Where(r =>
                            r.ParentType == nodeEntity.GetType() && r.ParentId.Equals(nodeEntity.GetId())).ToList();
                        foreach (var entryReference in node.Entry.References)
                        {
                            var refValue = entryReferences.FirstOrDefault(e =>
                                e.PropertyName == entryReference.Metadata.Name);
                            if (entryReference.CurrentValue is null)
                            {
                                if (refValue is not null)
                                {
                                    entryReference.Load();
                                    entryReference.CurrentValue = null;
                                }
                                else
                                {
                                    if (entryReference.Metadata is INavigation navigation)
                                    {
                                        foreach (var foreignKeyProperty in navigation.ForeignKey.Properties)
                                        {
                                            var fkProperty = node.Entry.Property(foreignKeyProperty.Name);
                                            fkProperty.IsModified = false;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (refValue is null)
                                {
                                    var refEntry = context.Entry(entryReference.CurrentValue);
                                    refEntry.State = refEntry.IsKeySet ? EntityState.Unchanged : EntityState.Added;
                                    entryReference.IsModified = true;
                                }
                            }
                        }


                        foreach (var collection in node.Entry.Collections)
                        {
                            var refValues = entryReferences.Where(e =>
                                e.PropertyName == collection.Metadata.Name).ToList();
                            if (refValues.Any())
                            {
                                var currentValue = collection.CurrentValue;
                                var current = collection.CurrentValue?.Cast<IEntity>().ToList();
                                if (current?.Count > 0)
                                {
                                    var originalIds = refValues.Select(v => v.Id);
                                    var currentIds = current.Select(v => v.GetId());
                                    if (currentIds.SequenceEqual(originalIds))
                                    {
                                        continue;
                                    }

                                    collection.CurrentValue = null;
                                    collection.Load();
                                    if (updateCollectionMethodInfo is null)
                                    {
                                        updateCollectionMethodInfo = typeof(EFRepository<,,>)
                                            .GetMethod(nameof(UpdateCollection),
                                                BindingFlags.NonPublic | BindingFlags.Static);
                                        if (updateCollectionMethodInfo is null)
                                        {
                                            throw new InvalidOperationException("Can't find method UpdateCollection");
                                        }
                                    }

                                    if (collection.CurrentValue is not null)
                                    {
                                        var method =
                                            updateCollectionMethodInfo.MakeGenericMethod(current.First().GetType(),
                                                collection.CurrentValue.GetType());
                                        collection.CurrentValue = method.Invoke(null,
                                                new object[] { collection.CurrentValue, currentValue!, context }) as
                                            IEnumerable;
                                        collection.IsModified = true;
                                    }
                                }
                                else
                                {
                                    collection.Load();
                                    if (collection.CurrentValue is ICollection<IEntity> entityCollection)
                                    {
                                        entityCollection.Clear();
                                    }
                                }
                            }
                        }
                    }
                });
                return Task.FromResult(true);
            }, cancellationToken);

        private static TCollection UpdateCollection<TElement, TCollection>(TCollection collection,
            IEnumerable<TElement> values,
            TDbContext dbContext)
            where TCollection : ICollection<TElement> where TElement : class, IEntity
        {
            var toDelete = new List<TElement>();
            foreach (var element in collection)
            {
                if (values.All(e => e.GetId()?.Equals(element.GetId()) != true))
                {
                    toDelete.Add(element);
                }
            }

            foreach (var element in toDelete)
            {
                collection.Remove(element);
            }

            foreach (var element in values)
            {
                var entry = GetTrackedEntity(dbContext, element);
                if (entry is null)
                {
                    dbContext.Add(element);
                }
                else
                {
                    if (collection.All(e => e.GetId()?.Equals(element.GetId()) != true))
                    {
                        collection.Add(element);
                    }
                }
            }

            return collection;
        }


        private void AnalyzeReferences(EntityEntry<IEntity> entry,
            List<EntityReference> loadedReferences)
        {
            foreach (var reference in entry.References)
            {
                if (reference.TargetEntry?.Entity is IEntity entityReference)
                {
                    var entityEntry = entry.Context.Entry(entityReference);
                    var id = new EntityReference(entry.Entity.GetType(), entry.Entity.GetId()!,
                        entityReference.GetType(),
                        entityReference.GetId()!, reference.Metadata.Name);
                    if (!loadedReferences.Contains(id))
                    {
                        loadedReferences.Add(id);
                        AnalyzeReferences(entityEntry, loadedReferences);
                    }
                }
            }

            foreach (var collection in entry.Collections)
            {
                if (collection.CurrentValue is not null)
                {
                    foreach (var entityReference in collection.CurrentValue.Cast<IEntity>())
                    {
                        var id = new EntityReference(entry.Entity.GetType(), entry.Entity.GetId()!,
                            entityReference.GetType(), entityReference.GetId()!, collection.Metadata.Name);
                        if (!loadedReferences.Contains(id))
                        {
                            var entityEntry = entry.Context.Entry(entityReference);
                            loadedReferences.Add(id);
                            AnalyzeReferences(entityEntry, loadedReferences);
                        }
                    }
                }
            }
        }

        private static EntityEntry? GetTrackedEntity<TTrackedEntity>(TDbContext trackingDbContext,
            TTrackedEntity trackedEntity)
            where TTrackedEntity : class, IEntity =>
            trackingDbContext.ChangeTracker.Entries().FirstOrDefault(x =>
                x.Entity is IEntity xEntity && xEntity.GetType() == trackedEntity.GetType() &&
                xEntity.GetId()?.Equals(trackedEntity.GetId()) == true);

        [PublicAPI]
        protected async Task<T> ExecuteDbContextOperationAsync<T>(Func<TDbContext, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            using (await repositoryLock.Lock.LockAsync(cancellationToken))
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

        public DbSet<T> Set<T>() where T : class => dbContext.Set<T>();

        protected override Task DoUpdateAsync(TEntity item, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        protected override Task DoDeleteAsync(TEntity item, CancellationToken cancellationToken = default) =>
            ExecuteDbContextOperationAsync(currentDbContext =>
            {
                currentDbContext.Remove(item);
                return Task.FromResult(true);
            }, cancellationToken);

        [PublicAPI]
        protected IQueryable<TEntity> GetBaseQuery() => dbContext.Set<TEntity>().AsQueryable();

        protected virtual IQueryable<TEntity> AddIncludes(IQueryable<TEntity> query) => query;

        protected override Task<EFRepositoryQuery<TEntity>> CreateRepositoryQueryAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EFRepositoryQuery<TEntity>(GetBaseQuery()));

        private IDbContextTransaction? GetCurrentTransaction() => dbContext.Database.CurrentTransaction;

        private record EntityReference(Type ParentType, object ParentId, Type Type, object Id, string PropertyName);

        protected override Task<PropertyChange[]> GetChangesAsync(TEntity item)
        {
            var modifiedStates = new[] { EntityState.Added, EntityState.Deleted, EntityState.Modified };
            var changes = new List<PropertyChange>();
            if (dbContext.ChangeTracker.HasChanges())
            {
                var entry = dbContext.Entry(item);
                if (entry.State != EntityState.Detached)
                {
                    foreach (var property in entry.Properties.Where(p => p.IsModified))
                    {
                        changes.Add(new PropertyChange(property.Metadata.Name, property.OriginalValue,
                            property.CurrentValue));
                    }

                    foreach (var entryReference in entry.References.Where(n =>
                        n.IsModified || (n.TargetEntry != null && modifiedStates.Contains(n.TargetEntry.State))))
                    {
                        changes.Add(new PropertyChange(entryReference.Metadata.Name, null,
                            entryReference.CurrentValue));
                    }

                    foreach (var entryCollection in entry.Collections.Where(c => c.IsLoaded || c.IsModified))
                    {
                        var hasChanges = false;
                        if (entryCollection.IsModified)
                        {
                            hasChanges = true;
                        }
                        else if (entryCollection.CurrentValue is not null)
                        {
                            foreach (var collectionElement in entryCollection.CurrentValue.Cast<object>())
                            {
                                var collectionEntry = dbContext.Entry(collectionElement);
                                if (modifiedStates.Contains(collectionEntry.State))
                                {
                                    hasChanges = true;
                                    break;
                                }
                            }
                        }

                        if (hasChanges)
                        {
                            changes.Add(new PropertyChange(entryCollection.Metadata.Name, null,
                                entryCollection.CurrentValue));
                        }
                    }
                }
            }

            return Task.FromResult(changes.ToArray());
        }
    }
}
