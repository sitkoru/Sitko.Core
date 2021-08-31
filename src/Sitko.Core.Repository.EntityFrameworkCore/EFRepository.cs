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
                            EFRepositoryHelper.CopyCollection(modifiedCollection.CurrentValue), ChangeType.Added);
                    }
                    else
                    {
                        var originalIds = originalValues.Select(v => v.EntityId).ToList();
                        if (!originalIds.OrderBy(id => id).SequenceEqual(ids.OrderBy(id => id)))
                        {
                            // use CopyCollection to prevent updating change value if model value changes
                            entityChange.AddChange(entryCollection.Metadata.Name, entryCollection.CurrentValue,
                                EFRepositoryHelper.CopyCollection(modifiedCollection.CurrentValue),
                                ChangeType.Modified);
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
                        entryCollection.CurrentValue.Cast<IEntity>().Any())
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
                                EFRepositoryHelper.CopyCollection(modifiedCollection.CurrentValue), ChangeType.Deleted);
                        }
                    }
                }
            }

            if (entityChange.Changes.Any())
            {
                changes.Add(entityChange);
            }
        }

        private static ChangeState HasChanges(EntityChange[]? entityChanges, IEntity entity, string propertyName,
            out PropertyChange? change)
        {
            change = default;
            if (entityChanges is null)
            {
                return ChangeState.Unknown;
            }

            var entityChange = entityChanges.FirstOrDefault(c => c.Entity.Equals(entity));
            var propertyChanges = entityChange?.Changes.Where(c => c.Name == propertyName).ToArray() ??
                                  Array.Empty<PropertyChange>();
            if (propertyChanges.Length > 0)
            {
                change = propertyChanges.First();
                return ChangeState.Changed;
            }

            return ChangeState.Unknown;
        }

        private static ChangeState HasChanges(EntityChange[]? entityChanges, IEntity entity) =>
            HasChanges(entityChanges, entity, out _);

        private static ChangeState HasChanges(EntityChange[]? entityChanges, IEntity entity,
            out PropertyChange[] changes)
        {
            if (entityChanges is null)
            {
                changes = Array.Empty<PropertyChange>();
                return ChangeState.Unknown;
            }

            var entityChange = entityChanges.FirstOrDefault(c => c.Entity.Equals(entity));
            changes = entityChange?.Changes ?? Array.Empty<PropertyChange>();
            return changes.Length > 0 ? ChangeState.Changed : ChangeState.UnChanged;
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

        private void AttachAllEntities(TDbContext context, IEntity entity, EntityChange[]? entityChanges)
        {
            var entities = new List<IEntity>();
            TraverseAndFillEntities(context, entity, entities);
            foreach (var processedEntity in entities)
            {
                var entry = context.Entry(processedEntity);
                entry.State = entry.IsKeySet ? EntityState.Unchanged : EntityState.Added;
            }

            foreach (var processedEntity in entities)
            {
                var entry = context.Entry(processedEntity);
                foreach (var collection in entry.Collections)
                {
                    if (collection.CurrentValue is not null)
                    {
                        var changeState = HasChanges(entityChanges, processedEntity, collection.Metadata.Name,
                            out var change);
                        if (changeState == ChangeState.Changed && change is not null)
                        {
                            EFRepositoryHelper.UpdateCollection(collection.CurrentValue,
                                (change.Value.CurrentValue as IEnumerable).Cast<IEntity>().ToList());
                        }
                    }
                }
            }
        }

        private void TraverseAndFillEntities(TDbContext context, IEntity entity, List<IEntity> entities)
        {
            var existingEntity = entities.FirstOrDefault(e => e.Equals(entity));
            if (existingEntity is not null)
            {
                if (existingEntity != entity && context.Entry(entity).IsKeySet)
                {
                    throw new InvalidOperationException($"Entity {entity} has multiple instances in graph");
                }

                return;
            }

            entities.Add(entity);
            var entry = context.Entry(entity);
            foreach (var reference in entry.References)
            {
                if (reference.CurrentValue is IEntity referenceCurrentValue)
                {
                    TraverseAndFillEntities(context, referenceCurrentValue, entities);
                }
            }

            foreach (var collection in entry.Collections)
            {
                if (collection.CurrentValue is not null)
                {
                    foreach (var collectionValue in collection.CurrentValue)
                    {
                        if (collectionValue is IEntity collectionItem)
                        {
                            TraverseAndFillEntities(context, collectionItem, entities);
                        }
                    }
                }
            }
        }

        protected override async Task<PropertyChange[]> DoUpdateAsync(TEntity entity, TEntity? oldEntity,
            CancellationToken cancellationToken = default) =>
            await ExecuteDbContextOperationAsync(async context =>
            {
                var entry = context.Entry(entity);
                if (entry.State == EntityState.Detached)
                {
                    // This is external entity, so we need to attach it to db context
                    // 1. If we have old entity - detect changes
                    var changes = oldEntity is null ? null : Compare(oldEntity, entity);
                    // 2. If we have entity Foo in graph and some other entity Bar has relation with Foo
                    // and we process Bar first and load Foo from DB, when we will process Foo from graph Foo from db would be already in DbContext.
                    // To prevent such duplicates we need to attach all graph entities to DbContext first.
                    AttachAllEntities(context, entity, changes);
                    // 3. Start walking entities graph
                    var entityEntry = context.Entry(entity as IEntity);
                    var processed = new List<IEntity>();
                    await AttachEntryAsync(entityEntry, changes, context, processed);
                    // 4. Entity attached. If we have old entity - return detected changes as flat list
                    if (changes is not null)
                    {
                        var entityChanges = new List<PropertyChange>();
                        foreach (var entityChange in changes)
                        {
                            foreach (var entityPropertyChange in entityChange.Changes)
                            {
                                var name = entityPropertyChange.Name;
                                if (!entityChange.Entity.Equals(entity))
                                {
                                    name = $"{entityChange.Entity}.{name}";
                                }

                                var change = new PropertyChange(name, entityPropertyChange.OriginalValue,
                                    entityPropertyChange.CurrentValue, entityPropertyChange.ChangeType);
                                entityChanges.Add(change);
                            }
                        }

                        return entityChanges.ToArray();
                    }
                }

                return await GetChangesAsync(entity);
            }, cancellationToken);

        private async Task AttachEntryAsync(EntityEntry<IEntity> entry, EntityChange[]? changes, TDbContext context,
            List<IEntity> processedEntities)
        {
            var entity = entry.Entity;
            if (processedEntities.Contains(entity))
            {
                // If we already process this entity - no need to do it again
                return;
            }

            // Load entity values from database
            Logger.LogDebug("Load entity {Type} [{Entity}] original value", entity.GetType(), entity.EntityId);
            var properties = await entry.GetDatabaseValuesAsync();
            if (properties is null)
            {
                // If database returns null - this is new entity, mark as added
                entry.State = EntityState.Added;
                // No need to do anything else with new entities
                return;
            }

            // Mark entity as processed to avoid loops
            processedEntities.Add(entity);

            var modifiedFks = new List<string>();
            // First we will detect changes in simple properties
            Logger.LogDebug("Process entity {Type} [{Entity}] properties", entity.GetType(), entity.EntityId);
            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsKey() || property.Metadata.IsShadowProperty())
                {
                    // Skip keys, cause they can't be changed
                    // Skip shadow properties, ef will updated them himself
                    continue;
                }

                // Compare current property value to one from database
                if (properties.TryGetValue<object>(property.Metadata.Name, out var value))
                {
                    if (!property.Metadata.GetValueComparer().Equals(property.CurrentValue, value))
                    {
                        Logger.LogDebug(
                            "Entity {Type} [{Entity}]. Property {Property} changed from {OldValue} to {NewValue}",
                            entity.GetType().Name, entity.EntityId, property.Metadata.Name,
                            value,
                            property.CurrentValue);
                        // Mark property as changed
                        property.IsModified = true;
                        property.OriginalValue = value;
                        if (property.Metadata.IsForeignKey())
                        {
                            // If property is foreign key - save it for reference changes detection later
                            modifiedFks.Add(property.Metadata.Name);
                        }
                    }
                }
            }

            // Next check references (one-to-one or one-to-many relations)
            Logger.LogDebug("Process entity {Type} [{Entity}] references", entity.GetType(), entity.EntityId);
            foreach (var entryReference in entry.References)
            {
                Logger.LogDebug("Process entity {Type} [{Entity}] reference {Reference}", entity.GetType(),
                    entity.EntityId, entryReference.Metadata.Name);
                // Find if reference foreign key was changed
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
                    // reference foreign key was already changed, ef will correctly update reference himself
                    continue;
                }

                var changeState = HasChanges(changes, entity, entryReference.Metadata.Name, out var change);
                if (changeState != ChangeState.UnChanged)
                {
                    // We don't have old entity or reference value has changed
                    if (entryReference.CurrentValue is null)
                    {
                        if (changeState == ChangeState.Changed)
                        {
                            // Current reference value is null, so it was deleted
                            // Load original value from db so ef will both sides of relation
                            await entryReference.LoadAsync();
                            // "Delete" it. Ef will update fk on both sides as necessary
                            entryReference.CurrentValue = null;
                            Logger.LogDebug(
                                "Entity {Type} [{Entity}]. Reference {Property} changed from {OldValue} to {NewValue}",
                                entity.GetType().Name, entity.EntityId, entryReference.Metadata.Name,
                                change?.OriginalValue, entryReference.CurrentValue);
                        }
                        else
                        {
                            entryReference.IsModified = false;
                            Logger.LogDebug(
                                "Entity {Type} [{Entity}]. Reference {Property} is null, but original value is unknown. Skip",
                                entity.GetType().Name, entity.EntityId, entryReference.Metadata.Name);
                        }
                    }
                    else
                    {
                        if (entryReference.CurrentValue is IEntity referencedEntity)
                        {
                            // No old entity to compare
                            if (changeState == ChangeState.Unknown)
                            {
                                // try to detect if reference value is back-reference produced by ef core navigation fixups
                                if (context.ChangeTracker.Entries()
                                        .Any(e => e.Entity.Equals(referencedEntity)) &&
                                    HasChanges(changes, referencedEntity) != ChangeState.Changed)
                                {
                                    // Probably it is, leave unmodified
                                    entryReference.IsModified = false;
                                    continue;
                                }
                            }

                            // If reference value is IEntity - attach it to graph
                            await AttachEntryAsync(context.Entry(referencedEntity), changes, context,
                                processedEntities);
                        }
                        else
                        {
                            // Else just mark as unchanged
                            entryReference.TargetEntry.State = EntityState.Unchanged;
                        }
                    }
                }
                else
                {
                    // Reference value was not changed
                    if (entryReference.CurrentValue is not null)
                    {
                        // If reference value is not null - we should process it
                        if (entryReference.CurrentValue is IEntity referencedEntity)
                        {
                            // Reference value is IEntity. Any changes?
                            if (HasChanges(changes, referencedEntity) == ChangeState.Changed)
                            {
                                // Yes, so full attach process
                                await AttachEntryAsync(context.Entry(referencedEntity), changes, context,
                                    processedEntities);
                            }
                            else
                            {
                                // No changes, mark as unchanged
                                context.Entry(referencedEntity).State = EntityState.Unchanged;
                            }
                        }
                        else
                        {
                            // Reference value is not IEntity, just mark as unchanged
                            entryReference.TargetEntry.State = EntityState.Unchanged;
                        }
                    }
                }
            }

            // Finally, let's process collections (many-to-one, many-to-many relations)
            Logger.LogDebug("Process entity {Type} [{Entity}] collections", entity.GetType(), entity.EntityId);
            foreach (var entryCollection in entry.Collections)
            {
                // Cast collection value to List of IEntity
                // ReSharper disable once PossibleMultipleEnumeration
                var currentValue = entryCollection.CurrentValue?.Cast<IEntity>().ToList();
                var changeState = HasChanges(changes, entity, entryCollection.Metadata.Name, out var change);
                if (changeState != ChangeState.UnChanged)
                {
                    // We don't have old entity or collection value has changed
                    if (currentValue is null || !currentValue.Any())
                    {
                        if (changeState == ChangeState.Changed)
                        {
                            // Collection value is null or empty, means it was cleared.
                            // Load original value from db so ef will both sides of relation
                            await entryCollection.LoadAsync();
                            // "Delete" it. Ef will update fk on both sides as necessary
                            entryCollection.CurrentValue = null;
                            Logger.LogDebug(
                                "Entity {Type} [{Entity}]. Collection {Property} changed from {OldValue} to {NewValue}",
                                entity.GetType().Name, entity.EntityId, entryCollection.Metadata.Name,
                                change?.OriginalValue, entryCollection.CurrentValue);
                        }
                        else
                        {
                            Logger.LogDebug(
                                "Entity {Type} [{Entity}]. Collection {Property} is null but original value is unknown. Skip",
                                entity.GetType().Name, entity.EntityId, entryCollection.Metadata.Name);
                            entryCollection.IsModified = false;
                        }
                    }
                    else
                    {
                        // Collection value is not empty. Here be dragons.
                        if (changeState == ChangeState.Unknown)
                        {
                            // No old entity so try to detect if all collection values are back-references produced by ef core navigation fixups
                            var allFound = true;
                            foreach (var collectionEntity in currentValue)
                            {
                                if (!context.ChangeTracker.Entries().Any(e => e.Entity.Equals(collectionEntity)) ||
                                    HasChanges(changes, collectionEntity) == ChangeState.Changed)
                                {
                                    allFound = false;
                                    break;
                                }
                            }

                            if (allFound)
                            {
                                // Probably they are. So leave unmodified
                                entryCollection.IsModified = false;
                                continue;
                            }
                        }

                        // Load original value from db so ef will knew both sides of relation
                        Logger.LogDebug("Load collection {Collection} original value", entryCollection.Metadata.Name);
                        entryCollection.CurrentValue = null;
                        await entryCollection.LoadAsync();

                        // Ef really likes when we manipulate existing collection, not replace it with new one.
                        // So we need to update current  collection value.
                        // Because it is some random IEnumerable - we need a bit of generic and reflection magic
                        // ReSharper disable once PossibleMultipleEnumeration
                        entryCollection.CurrentValue =
                            EFRepositoryHelper.UpdateCollection(entryCollection.CurrentValue!, currentValue);
                        // Trigger change detection to force ef to update all links
                        context.ChangeTracker.DetectChanges();
                        // Collection updated, now we can to process all collection values
                        foreach (var value in currentValue)
                        {
                            var existingEntity = context.Entry(value);
                            await AttachEntryAsync(existingEntity, changes, context, processedEntities);
                        }
                    }
                }
                else
                {
                    // Collection value was not changed
                    if (currentValue is not null && currentValue.Any())
                    {
                        // It is not empty, mark collection as loaded
                        entryCollection.IsLoaded = true;
                        // Check all collection elements for changes and process them
                        foreach (var collectionEntity in currentValue)
                        {
                            if (HasChanges(changes, collectionEntity) == ChangeState.Changed)
                            {
                                // Element has come changes - process
                                await AttachEntryAsync(context.Entry(collectionEntity), changes, context,
                                    processedEntities);
                            }
                            else
                            {
                                // Element is unchanged. Mark as such.
                                context.Entry(collectionEntity).State = EntityState.Unchanged;
                            }
                        }
                    }
                }
            }
        }

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
        private static MethodInfo? updateCollectionMethodInfo;
        private static MethodInfo? copyCollectionMethodInfo;

        public static IEnumerable UpdateCollection(IEnumerable collection, ICollection<IEntity> newValues)
        {
            if (updateCollectionMethodInfo is null)
            {
                updateCollectionMethodInfo = typeof(EFRepositoryHelper)
                    .GetMethod(nameof(UpdateCollection),
                        BindingFlags.NonPublic | BindingFlags.Static);
                if (updateCollectionMethodInfo is null)
                {
                    throw new InvalidOperationException("Can't find method UpdateCollection");
                }
            }

            var method =
                updateCollectionMethodInfo.MakeGenericMethod(collection.GetType().GetGenericArguments().First(),
                    collection.GetType());
            return (method.Invoke(null, new object?[] { collection, newValues }) as IEnumerable)!;
        }

        public static IEnumerable CopyCollection(IEnumerable collection)
        {
            if (copyCollectionMethodInfo is null)
            {
                copyCollectionMethodInfo = typeof(EFRepositoryHelper)
                    .GetMethod(nameof(CopyCollection),
                        BindingFlags.NonPublic | BindingFlags.Static);
                if (copyCollectionMethodInfo is null)
                {
                    throw new InvalidOperationException("Can't find method CopyCollection");
                }
            }

            var method = copyCollectionMethodInfo.MakeGenericMethod(collection.GetType().GetGenericArguments().First(),
                collection.GetType());
            return (method.Invoke(null, new object?[] { collection }) as IEnumerable)!;
        }

        private static TCollection UpdateCollection<TElement, TCollection>(TCollection? collection,
            ICollection<IEntity> values)
            where TCollection : ICollection<TElement>, new() where TElement : class, IEntity
        {
            collection ??= new TCollection();
            collection.Clear();

            foreach (var entity in values.Cast<TElement>())
            {
                collection.Add(entity);
            }

            return collection;
        }

        private static TCollection CopyCollection<TElement, TCollection>(TCollection collection)
            where TCollection : ICollection<TElement>, new() where TElement : class, IEntity
        {
            var newCollection = new TCollection();
            foreach (var element in collection)
            {
                newCollection.Add(element);
            }

            return newCollection;
        }
    }

    public enum ChangeState
    {
        Changed,
        UnChanged,
        Unknown
    }
}
