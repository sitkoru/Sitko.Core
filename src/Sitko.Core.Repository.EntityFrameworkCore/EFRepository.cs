using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly EFRepositoryContext<TEntity, TEntityPk, TDbContext> repositoryContext;
        private readonly EFRepositoryLock repositoryLock;

        protected EFRepository(EFRepositoryContext<TEntity, TEntityPk, TDbContext> repositoryContext) : base(
            repositoryContext)
        {
            this.repositoryContext = repositoryContext;
            dbContext = repositoryContext.DbContext;
            repositoryLock = repositoryContext.RepositoryLock;
        }

        private static bool Attach(TDbContext context, TEntity entity)
        {
            bool result;
            if (context.Set<TEntity>().Local.Contains(entity))
            {
                result = true;
                var entry = context.Entry(entity);
                foreach (var collection in entry.Collections)
                {
                    if (collection.CurrentValue is not null)
                    {
                        context.AttachRange(collection.CurrentValue);
                    }
                }

                foreach (var referenceEntry in entry.References)
                {
                    if (referenceEntry.CurrentValue is not null)
                    {
                        context.AttachRange(referenceEntry.CurrentValue);
                    }
                }
            }
            else
            {
                context.Set<TEntity>().Attach(entity);
                result = true;
            }

            return result;
        }

        private record EntityReference(Type ParentType, object ParentId, Type Type, object Id, string PropertyName);

        public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateExternalAsync(TEntity entity,
            TEntity? baseEntity,
            CancellationToken cancellationToken = default)
        {
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

                    var hasChanges = false;
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
                                hasChanges = true;
                            }
                        }
                    }

                    node.Entry.State = hasChanges ? EntityState.Modified : EntityState.Unchanged;
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
                                            if (fkProperty is not null)
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
                                var current = collection.CurrentValue.Cast<IEntity>().ToList();
                                if (current?.Cast<object>().Any() == true)
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
                                        updateCollectionMethodInfo = GetType().BaseType
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
                                                new object[] { collection.CurrentValue, currentValue, context }) as
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
            var result = await UpdateAsync(entity, cancellationToken);

            return result;
        }

        private MethodInfo? updateCollectionMethodInfo;

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
                    collection.Add(element);
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
        public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateExternalAsync(TEntity entity,
            Action<TEntity> update,
            CancellationToken cancellationToken = default)
        {
            if (Attach(dbContext, entity))
            {
                update(entity);
                return await UpdateAsync(entity, cancellationToken);
            }

            throw new InvalidOperationException("Can't attach entity to dbContext");
        }

        [PublicAPI]
        public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> UpdateExternalAsync(TEntity entity,
            Func<TEntity, Task> update,
            CancellationToken cancellationToken = default)
        {
            if (Attach(dbContext, entity))
            {
                await update(entity);
                return await UpdateAsync(entity, cancellationToken);
            }

            throw new InvalidOperationException("Can't attach entity to dbContext");
        }

        [PublicAPI]
        public async Task<AddOrUpdateOperationResult<TEntity, TEntityPk>> AddExternalAsync(TEntity entity,
            CancellationToken cancellationToken = default)
        {
            if (Attach(dbContext, entity))
            {
                return await AddAsync(entity, cancellationToken);
            }

            throw new InvalidOperationException("Can't attach entity to dbContext");
        }

        [PublicAPI]
        public async Task<bool> DeleteExternalAsync(TEntity entity,
            CancellationToken cancellationToken = default)
        {
            if (Attach(dbContext, entity))
            {
                return await DeleteAsync(entity, cancellationToken);
            }

            throw new InvalidOperationException("Can't attach entity to dbContext");
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

        protected override async Task<(PropertyChange[] changes, TEntity oldEntity)> GetChangesAsync(TEntity item)
        {
            using var scope = repositoryContext.CreateScope();
            var oldDbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
            var oldEntity = await oldDbContext.Set<TEntity>().FirstOrDefaultAsync(e => e.Id!.Equals(item.Id));
            if (oldEntity is null)
            {
                throw new InvalidOperationException("Old entity not found");
            }

            var entry = dbContext.Entry(item);
            var entityChanges = entry
                .Properties
                .Where(p => p.IsModified)
                .Select(p => new PropertyChange(p.Metadata.Name, p.OriginalValue, p.CurrentValue))
                .ToList();
            foreach (var collection in entry.Collections.Where(c => c.IsLoaded || c.IsModified))
            {
                var oldCollection = oldDbContext.Entry(oldEntity).Collections
                    .First(c => c.Metadata.Name == collection.Metadata.Name);
                await oldCollection.LoadAsync();
                if (oldCollection.CurrentValue?.Cast<object>().Count() !=
                    collection.CurrentValue?.Cast<object>().Count())
                {
                    entityChanges.Add(new PropertyChange(collection.Metadata.Name, oldCollection.CurrentValue,
                        collection.CurrentValue));
                    continue;
                }

                if (collection.CurrentValue?.Cast<object>().Any(collectionEntry =>
                    dbContext.Entry(collectionEntry).State != EntityState.Unchanged) == true)
                {
                    entityChanges.Add(new PropertyChange(collection.Metadata.Name, collection.CurrentValue,
                        collection.CurrentValue));
                }
            }

            foreach (var reference in entry.References)
            {
                if (reference.IsModified || reference.TargetEntry?.State == EntityState.Modified)
                {
                    entityChanges.Add(new PropertyChange(reference.Metadata.Name, reference.CurrentValue,
                        reference.CurrentValue));
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

        [PublicAPI]
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
