using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KellermanSoftware.CompareNetObjects;
using Sitko.Core.Repository;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.App.Blazor.Forms
{
    public abstract class BaseRepositoryForm<TEntity, TEntityPk, TRepository> : BaseForm<TEntity>
        where TEntity : class, IEntity<TEntityPk>, new() where TRepository : class, IRepository<TEntity, TEntityPk>
    {
        [Parameter] public TEntityPk? EntityId { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            if (EntitySnapshot != null && EntityId?.Equals(Entity.Id) == false)
            {
                await ResetAsync();
            }
        }

        protected override async Task<(bool IsNew, TEntity Entity)> GetEntityAsync()
        {
            var isNew = false;
            TEntity? entity;
            using var scope = CreateServicesScope();
            var repository = scope.ServiceProvider.GetRequiredService<TRepository>();
            if (EntityId is not null && default(TEntityPk)?.Equals(EntityId) == false)
            {
                entity = await repository.GetAsync(async q =>
                {
                    q.Where(e => e.Id!.Equals(EntityId));
                    await ConfigureQueryAsync(q);
                });
                if (entity is null)
                {
                    throw new InvalidOperationException($"Entity {EntityId} not found");
                }
            }
            else
            {
                entity = await repository.NewAsync();
                isNew = true;
            }

            return (isNew, entity);
        }

        protected virtual Task ConfigureQueryAsync(IRepositoryQuery<TEntity> query) => Task.CompletedTask;

        protected override async Task<FormSaveResult> AddAsync(TEntity entity)
        {
            using var scope = CreateServicesScope();
            var repository = scope.ServiceProvider.GetRequiredService<TRepository>();
            var result = await repository.AddAsync(entity);

            return new FormSaveResult(result.IsSuccess, result.ErrorsString);
        }

        protected override async Task<FormSaveResult> UpdateAsync(TEntity entity)
        {
            var (_, originalEntity) = await GetEntityAsync();
            using var scope = CreateServicesScope();
            var repository = scope.ServiceProvider.GetRequiredService<TRepository>();
            var result = await repository.UpdateAsync(entity, originalEntity);
            return new FormSaveResult(result.IsSuccess, result.ErrorsString);
        }

        protected override void ConfigureComparer(ComparisonConfig comparisonConfig)
        {
            base.ConfigureComparer(comparisonConfig);
            comparisonConfig.IgnoreCollectionOrder = true;
            comparisonConfig.CollectionMatchingSpec ??= new Dictionary<Type, IEnumerable<string>>();
            comparisonConfig.CollectionMatchingSpec.Add(typeof(IEntity), new[] { nameof(IEntity.EntityId) });
        }
    }
}
