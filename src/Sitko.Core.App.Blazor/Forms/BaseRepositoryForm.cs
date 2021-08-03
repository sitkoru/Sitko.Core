using System;
using System.Threading.Tasks;
using Sitko.Core.Repository;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.App.Blazor.Forms
{
    public abstract class BaseRepositoryForm<TEntity, TEntityPk, TRepository> : BaseForm<TEntity>
        where TEntity : class, IEntity<TEntityPk>, new() where TRepository : class, IRepository<TEntity, TEntityPk>
    {
        [Parameter] public TEntityPk? EntityId { get; set; }

        protected override async Task<(bool IsNew, TEntity Entity)> GetEntityAsync()
        {
            var isNew = false;
            TEntity? entity;
            using var scope = CreateServicesScope();
            var repository = scope.ServiceProvider.GetRequiredService<TRepository>();
            if (EntityId is not null && default(TEntityPk)?.Equals(EntityId) == false)
            {
                entity = await GetEntityAsync(repository, EntityId);
            }
            else
            {
                entity = await repository.NewAsync();
                isNew = true;
            }

            return (isNew, entity);
        }

        private async Task<TEntity> GetEntityAsync(TRepository repository, TEntityPk id)
        {
            var entity = await repository.GetAsync(async q =>
            {
                q.Where(e => e.Id!.Equals(id));
                await ConfigureQueryAsync(q);
            });
            if (entity is null)
            {
                throw new InvalidOperationException($"Entity {id} not found");
            }

            return entity;
        }

        protected virtual Task ConfigureQueryAsync(IRepositoryQuery<TEntity> query) => Task.CompletedTask;

        protected override async Task<FormSaveResult> AddAsync(TEntity entity)
        {
            using var scope = CreateServicesScope();
            var repository = scope.ServiceProvider.GetRequiredService<TRepository>();
            AddOrUpdateOperationResult<TEntity, TEntityPk> result;
            if (repository is IExternalRepository<TEntity, TEntityPk> externalRepository)
            {
                result = await externalRepository.AddExternalAsync(entity);
            }
            else
            {
                result = await repository.AddAsync(entity);
            }

            return new FormSaveResult(result.IsSuccess, result.ErrorsString);
        }

        protected override async Task<FormSaveResult> UpdateAsync(TEntity entity)
        {
            using var scope = CreateServicesScope();
            var repository = scope.ServiceProvider.GetRequiredService<TRepository>();
            AddOrUpdateOperationResult<TEntity, TEntityPk> result;
            if (repository is IExternalRepository<TEntity, TEntityPk> externalRepository)
            {
                result = await externalRepository.UpdateExternalAsync(entity, EntitySnapshot);
            }
            else
            {
                result = await repository.UpdateAsync(entity);
            }

            return new FormSaveResult(result.IsSuccess, result.ErrorsString);
        }

        public override async Task ResetAsync()
        {
            await InitializeAsync();
            await NotifyStateChangeAsync();
        }
    }
}
