using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Repository;

namespace Sitko.Core.App.Blazor.Forms
{
    public abstract class BaseRepositoryForm<TEntity, TEntityPk> : BaseForm<TEntity>
        where TEntity : class, IEntity<TEntityPk>, new()
    {
        protected IRepository<TEntity, TEntityPk> Repository { get; }

        protected BaseRepositoryForm(IRepository<TEntity, TEntityPk> repository,
            ILogger<BaseRepositoryForm<TEntity, TEntityPk>> logger) : base(logger) =>
            Repository = repository;

        private TEntityPk? EntityId { get; set; }

        public async Task InitializeAsync(TEntityPk? entityId)
        {
            TEntity? entity = null;
            if (entityId is not null && default(TEntityPk)?.Equals(entityId) == false)
            {
                EntityId = entityId;
                if (Entity is not null)
                {
                    await Repository.RefreshAsync(Entity);
                }

                entity = await Repository.GetAsync(async q =>
                {
                    q.Where(e => e.Id!.Equals(EntityId));
                    await ConfigureQueryAsync(q);
                });
                if (entity is null)
                {
                    throw new InvalidOperationException($"Entity {EntityId} not found");
                }
            }

            await InitializeAsync(entity);
        }

        protected virtual Task ConfigureQueryAsync(IRepositoryQuery<TEntity> query) => Task.CompletedTask;

        protected override async Task<FormSaveResult> AddAsync(TEntity entity)
        {
            var result = await Repository.AddAsync(entity);
            return new FormSaveResult(result.IsSuccess, result.ErrorsString);
        }

        protected override async Task<FormSaveResult> UpdateAsync(TEntity entity)
        {
            var result = await Repository.UpdateAsync(entity);
            return new FormSaveResult(result.IsSuccess, result.ErrorsString);
        }

        protected override Task<bool> DetectChangesAsync(TEntity entity) => Repository.HasChangesAsync(entity);

        public override async Task ResetAsync()
        {
            await InitializeAsync(EntityId);
            await NotifyStateChangeAsync();
        }
    }
}
