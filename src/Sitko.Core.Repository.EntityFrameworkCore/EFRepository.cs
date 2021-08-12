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
        BaseRepository<TEntity, TEntityPk, EFRepositoryQuery<TEntity>>
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

        private EntityChange[] Compare(TEntity firstEntity, TEntity secondEntity)
        {
            var changes = new List<EntityChange>();
            var processed = new List<IEntity>();
            var originalEntry = dbContext.Entry(firstEntity as IEntity);
            var modifiedEntry = dbContext.Entry(secondEntity as IEntity);
            ProcessEntryChanges(originalEntry, modifiedEntry, changes, processed);
            return changes.ToArray();
        }

        private void ProcessEntryChanges(EntityEntry<IEntity> originalEntry, EntityEntry<IEntity> modifiedEntry,
            List<EntityChange> changes, List<IEntity> processed)
        {
            if (processed.Contains(originalEntry.Entity))
            {
                return;
            }

            processed.Add(originalEntry.Entity);
            var entityChange = new EntityChange(originalEntry.Entity);
            foreach (var entryProperty in originalEntry.Properties)
            {
                if (entryProperty.Metadata.IsKey() || entryProperty.Metadata.IsShadowProperty())
                {
                    continue;
                }

                var modifiedProperty = modifiedEntry.Property(entryProperty.Metadata.Name);
                if (!entryProperty.Metadata.GetValueComparer()
                    .Equals(entryProperty.CurrentValue, modifiedProperty.CurrentValue))
                {
                    entityChange.AddChange(entryProperty.Metadata.Name, entryProperty.CurrentValue,
                        modifiedProperty.CurrentValue, ChangeType.Modified);
                }
            }

            foreach (var entryReference in originalEntry.References)
            {
                var modifiedReference = modifiedEntry.Reference(entryReference.Metadata.Name);
                if (modifiedReference.CurrentValue is not null &&
                    modifiedReference.CurrentValue is IEntity modifiedEntity)
                {
                    var referencedEntity = entryReference.CurrentValue as IEntity;
                    if (referencedEntity is null)
                    {
                        entityChange.AddChange(entryReference.Metadata.Name, entryReference.CurrentValue,
                            modifiedReference.CurrentValue, ChangeType.Added);
                        continue;
                    }

                    ProcessEntryChanges(dbContext.Entry(referencedEntity), dbContext.Entry(modifiedEntity), changes,
                        processed);
                    if (!referencedEntity.Equals(entryReference.CurrentValue))
                    {
                        entityChange.AddChange(entryReference.Metadata.Name, entryReference.CurrentValue,
                            modifiedReference.CurrentValue, ChangeType.Modified);
                    }
                }
                else
                {
                    if (entryReference.CurrentValue is not null)
                    {
                        entityChange.AddChange(entryReference.Metadata.Name, entryReference.CurrentValue,
                            modifiedReference.CurrentValue, ChangeType.Deleted);
                    }
                }
            }

            foreach (var entryCollection in originalEntry.Collections)
            {
                var modifiedCollection = modifiedEntry.Collection(entryCollection.Metadata.Name);
                if (modifiedCollection.CurrentValue is not null &&
                    modifiedCollection.CurrentValue.Cast<object?>().Any())
                {
                    var ids = modifiedCollection.CurrentValue.Cast<IEntity>().Select(v => v.EntityId).ToList();
                    var originalValues = entryCollection.CurrentValue?.Cast<IEntity>().ToList();
                    if (originalValues is null || !originalValues.Any())
                    {
                        entityChange.AddChange(entryCollection.Metadata.Name, entryCollection.CurrentValue,
                            modifiedCollection.CurrentValue, ChangeType.Added);
                    }
                    else
                    {
                        var originalIds = originalValues.Select(v => v.EntityId).ToList();
                        if (!originalIds.OrderBy(id => id).SequenceEqual(ids.OrderBy(id => id)))
                        {
                            entityChange.AddChange(entryCollection.Metadata.Name, entryCollection.CurrentValue,
                                modifiedCollection.CurrentValue, ChangeType.Modified);
                        }

                        var modifiedValues = modifiedCollection.CurrentValue?.Cast<IEntity>().ToList();
                        if (modifiedValues is not null)
                        {
                            foreach (var modifiedEntity in modifiedValues)
                            {
                                var originalValue =
                                    originalValues.FirstOrDefault(v => v.EntityId!.Equals(modifiedEntity.EntityId));
                                if (originalValue is not null)
                                {
                                    ProcessEntryChanges(dbContext.Entry(originalValue), dbContext.Entry(modifiedEntity),
                                        changes, processed);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (entryCollection.CurrentValue is not null &&
                        entryCollection.CurrentValue.Cast<IEntity>().Count() > 0)
                    {
                        // try to detect if all collection values are back-references produced by ef core navigation fixups
                        var hasNewRef = false;
                        foreach (var val in entryCollection.CurrentValue.Cast<IEntity>())
                        {
                            if (!processed.Any(e =>
                                e.GetType() == val.GetType() && e.EntityId!.Equals(val.EntityId)))
                            {
                                hasNewRef = true;
                            }
                        }

                        if (hasNewRef)
                        {
                            entityChange.AddChange(entryCollection.Metadata.Name, entryCollection.CurrentValue,
                                modifiedCollection.CurrentValue, ChangeType.Deleted);
                        }
                    }
                }
            }

            if (entityChange.Changes.Any())
            {
                changes.Add(entityChange);
            }
        }

        private static bool HasChanges(EntityChange[]? entityChanges, IEntity entity, string propertyName)
        {
            if (entityChanges is null)
            {
                return true;
            }

            var entityChange = entityChanges.FirstOrDefault(c => c.Entity.Equals(entity));
            return entityChange?.Changes.Any(c => c.Name == propertyName) == true;
        }

        private async Task<IEntity?> ProcessEntryAsync(EntityEntry<IEntity>? originalEntry,
            EntityEntry<IEntity> modifiedEntry,
            TDbContext context, List<IEntity> proccessedEntities, EntityChange[]? changes)
        {
            var entity = originalEntry?.Entity;
            var isNew = false;
            if (originalEntry is null)
            {
                if (modifiedEntry.IsKeySet)
                {
                    var pk = modifiedEntry.Properties.First(p => p.Metadata.IsPrimaryKey()).CurrentValue;
                    var originalEntity = await context.FindAsync(modifiedEntry.Entity.GetType(), pk);
                    if (originalEntity is null)
                    {
                        throw new InvalidOperationException(
                            $"Can't find entity {modifiedEntry.Entity.GetType()} with PK {pk}");
                    }

                    if (originalEntity is IEntity typedEntity)
                    {
                        entity = typedEntity;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Entity {modifiedEntry.Entity.GetType()} with PK {pk} is not IEntity");
                    }
                }
                else
                {
                    entity = modifiedEntry.Entity;
                    isNew = true;
                }

                originalEntry = context.Entry(entity);
            }
            else
            {
                if (proccessedEntities.Contains(originalEntry.Entity))
                {
                    return originalEntry.Entity;
                }
            }

            if (isNew)
            {
                return entity;
            }

            if (entity is null)
            {
                throw new InvalidOperationException("Entity is null");
            }

            proccessedEntities.Add(entity);
            var modifiedFks = new List<string>();
            foreach (var entryProperty in originalEntry.Properties)
            {
                if (entryProperty.Metadata.IsKey() || entryProperty.Metadata.IsShadowProperty())
                {
                    continue;
                }

                var modifiedProperty = modifiedEntry.Property(entryProperty.Metadata.Name);
                if (!entryProperty.Metadata.GetValueComparer()
                    .Equals(entryProperty.CurrentValue, modifiedProperty.CurrentValue))
                {
                    Logger.LogDebug(
                        "Entity {Type} [{Entity}]. Property {Property} changed from {OldValue} to {NewValue}",
                        entity.GetType().Name, entity.EntityId, entryProperty.Metadata.Name, entryProperty.CurrentValue,
                        modifiedProperty.CurrentValue);
                    entryProperty.CurrentValue = modifiedProperty.CurrentValue;
                    if (entryProperty.Metadata.IsForeignKey())
                    {
                        modifiedFks.Add(entryProperty.Metadata.Name);
                    }
                }
            }

            foreach (var entryReference in originalEntry.References)
            {
                var changedViaProperty = false;
                if (entryReference.Metadata is INavigation navigation)
                {
                    foreach (var foreignKeyProperty in navigation.ForeignKey.Properties)
                    {
                        if (modifiedFks.Contains(foreignKeyProperty.Name))
                        {
                            changedViaProperty = true;
                        }
                    }
                }

                if (changedViaProperty)
                {
                    continue;
                }

                var modifiedReference = modifiedEntry.Reference(entryReference.Metadata.Name);
                if (modifiedReference.CurrentValue is not null &&
                    modifiedReference.CurrentValue is IEntity modifiedEntity)
                {
                    var referenceEntry = entryReference.CurrentValue is null
                        ? null
                        : EFRepositoryHelper.GetTrackedEntity(context, (IEntity)entryReference.CurrentValue);
                    var processedEntity = await ProcessEntryAsync(referenceEntry,
                        context.Entry(modifiedEntity), context, proccessedEntities, changes);
                    if (processedEntity != entryReference.CurrentValue)
                    {
                        Logger.LogDebug(
                            "Entity {Type} [{Entity}]. Reference {Property} changed from {OldValue} to {NewValue}",
                            entity.GetType().Name, entity.EntityId, entryReference.Metadata.Name,
                            entryReference.CurrentValue, processedEntity);
                        entryReference.CurrentValue = processedEntity;
                    }
                }
                else
                {
                    if (HasChanges(changes, originalEntry.Entity,
                        entryReference.Metadata.Name))
                    {
                        await entryReference.LoadAsync();
                        if (entryReference.CurrentValue is not null)
                        {
                            Logger.LogDebug(
                                "Entity {Type} [{Entity}]. Reference {Property} changed from {OldValue} to null",
                                entity.GetType().Name, entity.EntityId, entryReference.Metadata.Name,
                                entryReference.CurrentValue);
                            entryReference.CurrentValue = null;
                        }
                    }
                }
            }

            foreach (var entryCollection in originalEntry.Collections)
            {
                // ReSharper disable once PossibleMultipleEnumeration
                var currentValue = entryCollection.CurrentValue?.Cast<IEntity>().ToList();
                var modifiedCollection = modifiedEntry.Collection(entryCollection.Metadata.Name);
                var modifiedValue = modifiedCollection.CurrentValue?.Cast<IEntity>().ToList();
                if (modifiedValue is not null && modifiedValue.Any())
                {
                    var ids = modifiedValue.Select(v => v.EntityId).ToList();
                    var hasChanges = HasChanges(changes,
                        originalEntry.Entity, entryCollection.Metadata.Name);
                    if (!entryCollection.IsLoaded)
                    {
                        await entryCollection.LoadAsync();
                        // ReSharper disable once PossibleMultipleEnumeration
                        currentValue = entryCollection.CurrentValue?.Cast<IEntity>().ToList();
                        Logger.LogDebug("Entity {Type} [{Entity}]. Collection {Property} loaded",
                            entity.GetType().Name, entity.EntityId, entryCollection.Metadata.Name);
                    }
                    if (!hasChanges)
                    {
                        if (currentValue is not null)
                        {
                            var dbIds = currentValue.Select(v => v.EntityId).ToList();
                            if (dbIds.Any())
                            {
                                var first = currentValue.First();
                                if (!dbIds.OrderBy(id => id).SequenceEqual(ids.OrderBy(id => id)))
                                {
                                    if (ids.Intersect(dbIds).Count() == ids.Count)
                                    {
                                        foreach (var id in ids)
                                        {
                                            if (!proccessedEntities.Any(e =>
                                                e.GetType() == first.GetType() && e.EntityId!.Equals(id)))
                                            {
                                                hasChanges = true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        hasChanges = true;
                                    }
                                }
                            }
                            else
                            {
                                hasChanges = true;
                            }

                            if (hasChanges)
                            {
                                Logger.LogDebug(
                                    "Entity {Type} [{Entity}]. Collection {Property} changed from {OldValue} to {NewValue}",
                                    entity.GetType().Name, entity.EntityId, entryCollection.Metadata.Name, dbIds, ids);
                            }
                        }
                        else
                        {
                            hasChanges = true;
                        }
                    }


                    if (hasChanges)
                    {
                        var values = new List<IEntity>();
                        foreach (var value in modifiedValue)
                        {
                            var existingEntity = EFRepositoryHelper.GetTrackedEntity(context, value);
                            var processedEntity =
                                await ProcessEntryAsync(existingEntity, context.Entry(value), context,
                                    proccessedEntities,
                                    changes);
                            if (processedEntity is not null)
                            {
                                values.Add(processedEntity);
                            }
                        }

                        entryCollection.CurrentValue = UpdateCollection(
                            modifiedCollection.CurrentValue!.GetType().GetGenericArguments().First(),
                            // ReSharper disable once PossibleMultipleEnumeration
                            modifiedCollection.CurrentValue!.GetType(), context, entryCollection.CurrentValue, values);
                        entryCollection.IsModified = true;
                    }
                }
                else
                {
                    if (HasChanges(changes, entity, entryCollection.Metadata.Name) && currentValue is not null &&
                        currentValue.Any())
                    {
                        Logger.LogDebug(
                            "Entity {Type} [{Entity}]. Collection {Property} changed from {OldValue} to {NewValue}",
                            entity.GetType().Name, entity.EntityId, entryCollection.Metadata.Name,
                            entryCollection.CurrentValue, modifiedCollection.CurrentValue);
                        entryCollection.CurrentValue = modifiedCollection.CurrentValue;
                    }
                }
            }

            return entity;
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

        private IEnumerable UpdateCollection(Type elementType, Type collectionType, TDbContext context,
            IEnumerable? oldValues, ICollection<IEntity> newValues)
        {
            if (updateCollectionMethodInfo is null)
            {
                updateCollectionMethodInfo = typeof(EFRepositoryHelper)
                    .GetMethod(nameof(EFRepositoryHelper.UpdateCollection),
                        BindingFlags.Public | BindingFlags.Static);
                if (updateCollectionMethodInfo is null)
                {
                    throw new InvalidOperationException("Can't find method UpdateCollection");
                }
            }

            var method = updateCollectionMethodInfo.MakeGenericMethod(elementType,
                collectionType);
            return (method.Invoke(this, new object?[] { oldValues, newValues, context }) as IEnumerable)!;
        }

        public override Task RefreshAsync(TEntity entity, CancellationToken cancellationToken = default) =>
            dbContext.Entry(entity).ReloadAsync(cancellationToken);

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

        protected override async Task DoAddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
            await ExecuteDbContextOperationAsync(
                currentDbContext =>
                {
                    currentDbContext.ChangeTracker.TrackGraph(entity, node =>
                    {
                        var properties = node.Entry.GetDatabaseValues();
                        if (properties is null)
                        {
                            node.Entry.State = EntityState.Added;
                            return;
                        }

                        node.Entry.State = EntityState.Unchanged;
                    });
                    return currentDbContext.AddAsync(entity, cancellationToken).AsTask();
                },
                cancellationToken);

        public DbSet<T> Set<T>() where T : class => dbContext.Set<T>();

        protected override async Task<PropertyChange[]> DoUpdateAsync(TEntity entity, TEntity? oldEntity,
            CancellationToken cancellationToken = default) =>
            await ExecuteDbContextOperationAsync(async context =>
            {
                var entry = context.Entry(entity);
                if (entry.State == EntityState.Detached)
                {
                    var changes = oldEntity is null ? null : Compare(oldEntity, entity);
                    var modifiedEntry = context.Entry(entity as IEntity);
                    var processed = new List<IEntity>();
                    var result = await ProcessEntryAsync(null, modifiedEntry, context, processed, changes) as TEntity ??
                                 throw new InvalidOperationException("Entity result is null");
                    Logger.LogDebug("External entity {Entity} processed", result);
                    return await GetChangesAsync(result);
                }

                return await GetChangesAsync(entity);
            }, cancellationToken);

        protected override Task DoDeleteAsync(TEntity entity, CancellationToken cancellationToken = default) =>
            ExecuteDbContextOperationAsync(currentDbContext =>
            {
                var entry = currentDbContext.Remove(entity);
                return Task.FromResult(entry.State == EntityState.Deleted);
            }, cancellationToken);

        [PublicAPI]
        protected IQueryable<TEntity> GetBaseQuery() => dbContext.Set<TEntity>().AsQueryable();

        protected virtual IQueryable<TEntity> AddIncludes(IQueryable<TEntity> query) => query;

        protected override Task<EFRepositoryQuery<TEntity>> CreateRepositoryQueryAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EFRepositoryQuery<TEntity>(GetBaseQuery()));

        private IDbContextTransaction? GetCurrentTransaction() => dbContext.Database.CurrentTransaction;

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
                            property.CurrentValue, ChangeType.Modified));
                    }

                    foreach (var entryReference in entry.References.Where(n =>
                        n.IsModified || (n.TargetEntry != null && modifiedStates.Contains(n.TargetEntry.State))))
                    {
                        changes.Add(new PropertyChange(entryReference.Metadata.Name, null,
                            entryReference.CurrentValue, ChangeType.Modified));
                    }

                    foreach (var entryCollection in entry.Collections.Where(c => c.IsLoaded || c.IsModified))
                    {
                        var hasChanges = false;
                        if (entryCollection.IsModified && !modifiedStates.Contains(entryCollection.EntityEntry.State))
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
                                entryCollection.CurrentValue, ChangeType.Modified));
                        }
                    }
                }
            }

            return Task.FromResult(changes.ToArray());
        }
    }

    public static class EFRepositoryHelper
    {
        public static EntityEntry<IEntity>? GetTrackedEntity<TTrackedEntity>(DbContext trackingDbContext,
            TTrackedEntity trackedEntity)
            where TTrackedEntity : class, IEntity
        {
            var entry = trackingDbContext.ChangeTracker.Entries().FirstOrDefault(x =>
                x.Entity is IEntity xEntity && xEntity.GetType() == trackedEntity.GetType() &&
                xEntity.EntityId?.Equals(trackedEntity.EntityId) == true);
            if (entry is not null)
            {
                return trackingDbContext.Entry(entry.Entity as IEntity) as EntityEntry<IEntity>;
            }

            return null;
        }

        public static TCollection UpdateCollection<TElement, TCollection>(TCollection? collection,
            ICollection<IEntity> values,
            DbContext context)
            where TCollection : ICollection<TElement>, new() where TElement : class, IEntity
        {
            var toDelete = new List<TElement>();
            collection ??= new TCollection();
            foreach (var element in collection)
            {
                if (values.All(e => e.EntityId?.Equals(element.EntityId) != true))
                {
                    toDelete.Add(element);
                }
            }

            foreach (var element in toDelete)
            {
                collection.Remove(element);
            }

            var ids = collection.Select(e => e.EntityId).ToList();
            foreach (var element in values.Cast<TElement>())
            {
                var entry = GetTrackedEntity(context, element);
                if (entry is null)
                {
                    context.Add(element);
                    collection.Add(element);
                }
                else
                {
                    var elementId = element.EntityId;
                    if (!ids.Any(id => id!.Equals(elementId)))
                    {
                        collection.Add(element);
                    }
                }
            }

            return collection;
        }
    }
}
