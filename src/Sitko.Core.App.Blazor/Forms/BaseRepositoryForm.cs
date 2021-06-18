using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitko.Core.Repository;

namespace Sitko.Core.App.Blazor.Forms
{
    public abstract class BaseRepositoryForm<TEntity, TEntityPk> : BaseForm<TEntity>
        where TEntity : class, IEntity<TEntityPk>, new()
    {
        protected readonly IRepository<TEntity, TEntityPk> Repository;

        protected BaseRepositoryForm(IRepository<TEntity, TEntityPk> repository,
            ILogger<BaseRepositoryForm<TEntity, TEntityPk>> logger) : base(logger)
        {
            Repository = repository;
        }

        private TEntityPk? EntityId { get; set; }

        public async Task InitializeAsync(TEntityPk? entityId)
        {
            TEntity? entity = null;
            if (entityId is not null && default(TEntityPk)?.Equals(entityId) == false)
            {
                EntityId = entityId;
                entity = await Repository.GetByIdAsync(entityId);
                if (entity is null)
                {
                    throw new Exception($"Entity {EntityId} not found");
                }
            }

            await InitializeAsync(entity);
        }

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

        protected override async Task<bool> DetectChangesAsync()
        {
            if (Entity is not null && !IsNew)
            {
                await MapEntityAsync(Entity);
                return Repository.GetChanges(Entity, new TEntity()).Length > 0;
            }

            return true;
        }
    }
}
