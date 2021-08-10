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
                    var ids = modifiedCollection.CurrentValue.Cast<IEntity>().Select(v => v.GetId()).ToList();
                    var originalValues = entryCollection.CurrentValue?.Cast<IEntity>().ToList();
                    if (originalValues is null || !originalValues.Any())
                    {
                        entityChange.AddChange(entryCollection.Metadata.Name, entryCollection.CurrentValue,
                            modifiedCollection.CurrentValue, ChangeType.Added);
                    }
                    else
                    {
                        var originalIds = originalValues.Select(v => v.GetId()).ToList();
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
                                    originalValues.FirstOrDefault(v => v.GetId()!.Equals(modifiedEntity.GetId()));
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
                                e.GetType() == val.GetType() && e.GetId()!.Equals(val.GetId())))
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

        public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateExternalAsync(TEntity entity,
            TEntity? originalEntity = null, CancellationToken cancellationToken = default)
        {
            await ExecuteDbContextOperationAsync(async context =>
            {
                var changes = originalEntity is null ? null : Compare(originalEntity, entity);
                var modifiedEntry = context.Entry(entity as IEntity);
                var processed = new List<IEntity>();
                originalEntity =
                    await ProcessEntryAsync(null, modifiedEntry, context, processed, changes) as TEntity;
                Logger.LogInformation("External entity processed");
                return true;
            }, cancellationToken);
            if (originalEntity is null)
            {
                throw new InvalidOperationException("Entity result is null");
            }

            return await UpdateAsync(originalEntity, cancellationToken);
        }

        private bool HasChanges(EntityChange[]? entityChanges, IEntity entity, string propertyName)
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
                    Logger.LogInformation(
                        "Entity {Type} [{Entity}]. Property {Property} changed from {OldValue} to {NewValue}",
                        entity.GetType().Name, entity.GetId(), entryProperty.Metadata.Name, entryProperty.CurrentValue,
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
                        Logger.LogInformation(
                            "Entity {Type} [{Entity}]. Reference {Property} changed from {OldValue} to {NewValue}",
                            entity.GetType().Name, entity.GetId(), entryReference.Metadata.Name,
                            entryReference.CurrentValue, processedEntity);
                        entryReference.CurrentValue = processedEntity;
                    }
                }
                else
                {
                    if (HasChanges(changes, originalEntry.Entity, entryReference.Metadata.Name))
                    {
                        await entryReference.LoadAsync();
                        if (entryReference.CurrentValue is not null)
                        {
                            Logger.LogInformation(
                                "Entity {Type} [{Entity}]. Reference {Property} changed from {OldValue} to null",
                                entity.GetType().Name, entity.GetId(), entryReference.Metadata.Name,
                                entryReference.CurrentValue);
                            entryReference.CurrentValue = null;
                        }
                    }
                }
            }

            foreach (var entryCollection in originalEntry.Collections)
            {
                var modifiedCollection = modifiedEntry.Collection(entryCollection.Metadata.Name);
                if (modifiedCollection.CurrentValue is not null &&
                    modifiedCollection.CurrentValue.Cast<object?>().Any())
                {
                    var ids = modifiedCollection.CurrentValue.Cast<IEntity>().Select(v => v.GetId()).ToList();
                    var hasChanges = HasChanges(changes, originalEntry.Entity, entryCollection.Metadata.Name);
                    if (!entryCollection.IsLoaded)
                    {
                        await entryCollection.LoadAsync();
                        Logger.LogInformation("Entity {Type} [{Entity}]. Collection {Property} loaded",
                            entity.GetType().Name, entity.GetId(), entryCollection.Metadata.Name);
                    }

                    if (!hasChanges)
                    {
                        if (entryCollection.CurrentValue is not null)
                        {
                            var dbIds = entryCollection.CurrentValue.Cast<IEntity>().Select(v => v.GetId()).ToList();
                            if (dbIds.Any())
                            {
                                var first = entryCollection.CurrentValue.Cast<IEntity>().First();
                                if (!dbIds.OrderBy(id => id).SequenceEqual(ids.OrderBy(id => id)))
                                {
                                    if (ids.Intersect(dbIds).Count() == ids.Count)
                                    {
                                        foreach (var id in ids)
                                        {
                                            if (!proccessedEntities.Any(e =>
                                                e.GetType() == first.GetType() && e.GetId()!.Equals(id)))
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
                                Logger.LogInformation(
                                    "Entity {Type} [{Entity}]. Collection {Property} changed from {OldValue} to {NewValue}",
                                    entity.GetType().Name, entity.GetId(), entryCollection.Metadata.Name, dbIds, ids);
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
                        foreach (var value in modifiedCollection.CurrentValue.Cast<IEntity>())
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
                            modifiedCollection.CurrentValue.GetType().GetGenericArguments().First(),
                            modifiedCollection.CurrentValue.GetType(), context, entryCollection.CurrentValue, values);
                        entryCollection.IsModified = true;
                    }
                }
                else
                {
                    if (HasChanges(changes, entity, entryCollection.Metadata.Name) &&
                        entryCollection.CurrentValue is not null &&
                        entryCollection.CurrentValue.Cast<IEntity>().Count() > 0)
                    {
                        Logger.LogInformation(
                            "Entity {Type} [{Entity}]. Collection {Property} changed from {OldValue} to {NewValue}",
                            entity.GetType().Name, entity.GetId(), entryCollection.Metadata.Name,
                            entryCollection.CurrentValue, modifiedCollection.CurrentValue);
                        entryCollection.CurrentValue = modifiedCollection.CurrentValue;
                    }
                }
            }

            return entity;
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

        private IEnumerable UpdateCollection(Type elementType, Type collectionType, TDbContext context,
            IEnumerable? oldValues, IEnumerable newValues)
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
                        var existedEntry = EFRepositoryHelper.GetTrackedEntity(context, nodeEntity);
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
                                            if (fkProperty.CurrentValue is null)
                                            {
                                                fkProperty.IsModified = false;
                                            }
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
                                    if (collection.CurrentValue is not null)
                                    {
                                        collection.CurrentValue = UpdateCollection(current.First().GetType(),
                                            collection.CurrentValue.GetType(), context, collection.CurrentValue,
                                            currentValue!);
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

        private record EntityReference(Type ParentType, object ParentId, Type Type, object Id, string PropertyName);
    }

    public static class EFRepositoryHelper
    {
        public static EntityEntry<IEntity>? GetTrackedEntity<TTrackedEntity>(DbContext trackingDbContext,
            TTrackedEntity trackedEntity)
            where TTrackedEntity : class, IEntity
        {
            var entry = trackingDbContext.ChangeTracker.Entries().FirstOrDefault(x =>
                x.Entity is IEntity xEntity && xEntity.GetType() == trackedEntity.GetType() &&
                xEntity.GetId()?.Equals(trackedEntity.GetId()) == true);
            if (entry is not null)
            {
                return trackingDbContext.Entry(entry.Entity as IEntity) as EntityEntry<IEntity>;
            }

            return null;
        }

        public static TCollection UpdateCollection<TElement, TCollection>(TCollection? collection,
            IEnumerable<IEntity> values,
            DbContext context)
            where TCollection : ICollection<TElement>, new() where TElement : class, IEntity
        {
            var toDelete = new List<TElement>();
            collection ??= new();
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

            var ids = collection.Select(e => e.GetId()).ToList();
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
                    var elementId = element.GetId();
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
