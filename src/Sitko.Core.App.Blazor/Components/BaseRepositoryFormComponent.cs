using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sitko.Core.Repository;

namespace Sitko.Core.App.Blazor.Components
{
    public abstract class
        BaseRepositoryFormComponent<TEntity, TEntityPk, TFormModel> : BaseFormComponent<TEntity, TFormModel>
        where TFormModel : BaseFormModel<TEntity> where TEntity : class, IEntity<TEntityPk>, new()
    {
        [Parameter] public TEntityPk? EntityId { get; set; }

        protected IRepository<TEntity, TEntityPk> Repository => GetService<IRepository<TEntity, TEntityPk>>();

        protected override async Task<TEntity> GetEntityAsync()
        {
            if (IsNew)
            {
                var entity = new TEntity();
                await InitializeEntityAsync(entity);
                return entity;
            }
            else
            {
                if (EntityId is null)
                {
                    throw new ArgumentException("EntityId is null");
                }

                var entity = await Repository.GetByIdAsync(EntityId);
                if (entity is null)
                {
                    throw new Exception($"Entity {EntityId} not found");
                }

                return entity;
            }
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

        protected virtual Task InitializeEntityAsync(TEntity entity)
        {
            return Task.CompletedTask;
        }
    }
}
